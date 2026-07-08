using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using AppCoins.Internal;
using Product = UnityEngine.Purchasing.Product;
using ACPurchase = AppCoins.Internal.Purchase;

namespace AppCoins.Unity
{
    /// <summary>
    /// Unity IAP v5 custom store backed by the AppCoins iOS SDK.
    ///
    /// Each override forwards to the internal native bridge
    /// (<see cref="AppCoinsNativeBridge"/>) and marshals the result onto the
    /// Unity main thread before invoking the corresponding base callback. The
    /// native Swift/Obj-C++ bridge and P/Invoke layer are reused unchanged.
    /// </summary>
    public class AppCoinsStore : Store
    {
        /// <summary>Current connection state, surfaced through the store wrapper.</summary>
        public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;

        #region Connect

        public override void Connect()
        {
            ConnectionState = ConnectionState.Connecting;
            AppCoinsNativeBridge.Initialize();

            // isAvailable is checked by AppCoinsIAP.ConfigureStoreAsync (Automatic mode).
            // By the time Connect() is called the store has been chosen deliberately
            // (Automatic resolved it, or the developer forced Aptoide/Apple), so connect
            // unconditionally — re-checking here would break the explicit modes.
            //
            // Signal success synchronously. Unity IAP already invokes Connect() on the
            // main thread, and there is no async work left to await, so there is no reason
            // to defer via the dispatcher. Firing here completes StoreController.Connect()'s
            // Task within the same pump, so FetchProducts() is safe immediately after await.
            ConnectionState = ConnectionState.Connected;
            ConnectCallback?.OnStoreConnectionSucceeded();
        }

        #endregion

        #region Fetch products

        public override void FetchProducts(IReadOnlyCollection<ProductDefinition> products)
        {
            var skus = products.Select(p => p.storeSpecificId).ToArray();

            RunAsync(async () =>
            {
                var result = await AppCoinsNativeBridge.GetProducts(skus);
                Dispatch(() =>
                {
                    if (result != null && result.IsSuccess && result.Value != null)
                    {
                        var descriptions = new List<ProductDescription>(result.Value.Length);
                        foreach (var p in result.Value)
                        {
                            var metadata = new ProductMetadata(
                                p.PriceLabel,
                                p.Title,
                                p.Description,
                                p.PriceCurrency,
                                ParsePrice(p.PriceValue));
                            descriptions.Add(new ProductDescription(p.Sku, metadata));
                        }
                        ProductsCallback?.OnProductsFetched(descriptions);
                    }
                    else
                    {
                        var (reason, message) = MapProductFetchFailure(result?.Error);
                        ProductsCallback?.OnProductsFetchFailed(
                            new ProductFetchFailureDescription(reason, message));
                    }
                });
            });
        }

        #endregion

        #region Purchase

        public override void Purchase(ICart cart)
        {
            string sku = FirstStoreSpecificId(cart);
            if (string.IsNullOrEmpty(sku))
            {
                Dispatch(() => PurchaseCallback?.OnPurchaseFailed(
                    new FailedOrder(cart, PurchaseFailureReason.ProductUnavailable, "Empty cart or unknown product.")));
                return;
            }

            RunAsync(async () =>
            {
                var result = await AppCoinsNativeBridge.Purchase(sku, string.Empty);
                Dispatch(() => HandlePurchaseResult(cart, result));
            });
        }

        private void HandlePurchaseResult(ICart cart, AppCoinsSDKPurchaseResult result)
        {
            if (result == null)
            {
                PurchaseCallback?.OnPurchaseFailed(
                    new FailedOrder(cart, PurchaseFailureReason.Unknown, "No response from AppCoins."));
                return;
            }

            switch (result.State)
            {
                case AppCoinsNativeBridge.PURCHASE_STATE_SUCCESS:
                {
                    var purchase = result.Value?.Purchase;
                    var receipt = AppCoinsReceipt.Build(purchase, result.Value?.VerificationResult);
                    var info = new AppCoinsOrderInfo(receipt, purchase?.OrderUID);
                    PurchaseCallback?.OnPurchaseSucceeded(new PendingOrder(cart, info));
                    break;
                }

                case AppCoinsNativeBridge.PURCHASE_STATE_PENDING:
                {
                    // Payment authorized but not yet settled -> Unity deferred order.
                    var info = new AppCoinsOrderInfo(string.Empty, null);
                    PurchaseCallback?.OnPurchaseDeferred(new DeferredOrder(cart, info));
                    break;
                }

                case AppCoinsNativeBridge.PURCHASE_STATE_USER_CANCELLED:
                    PurchaseCallback?.OnPurchaseFailed(
                        new FailedOrder(cart, PurchaseFailureReason.UserCancelled, "User cancelled the purchase."));
                    break;

                default: // PURCHASE_STATE_FAILED
                {
                    var (reason, details) = MapPurchaseFailure(result.Error);
                    PurchaseCallback?.OnPurchaseFailed(new FailedOrder(cart, reason, details));
                    break;
                }
            }
        }

        /// <summary>
        /// Surfaces an AppCoins purchase-intent (indirect / deep-link purchase)
        /// through the standard Unity flow as a pending order.
        /// </summary>
        internal void SurfacePendingOrder(ICart cart, ACPurchase purchase, string verificationResult)
        {
            var receipt = AppCoinsReceipt.Build(purchase, verificationResult);
            var info = new AppCoinsOrderInfo(receipt, purchase?.OrderUID);
            Dispatch(() => PurchaseCallback?.OnPurchaseSucceeded(new PendingOrder(cart, info)));
        }

        #endregion

        #region Finish transaction (confirm / consume)

        public override void FinishTransaction(PendingOrder pendingOrder)
        {
            string sku = FirstStoreSpecificId(pendingOrder?.CartOrdered);
            string transactionId = pendingOrder?.Info?.TransactionID;

            if (string.IsNullOrEmpty(sku))
            {
                Dispatch(() => ConfirmCallback?.OnConfirmOrderFailed(
                    new FailedOrder(pendingOrder, PurchaseFailureReason.PurchaseMissing,
                        "Unknown product; cannot confirm order.")));
                return;
            }

            RunAsync(async () =>
            {
                var result = await AppCoinsNativeBridge.ConsumePurchase(sku);
                Dispatch(() =>
                {
                    if (result != null && result.IsSuccess)
                    {
                        ConfirmCallback?.OnConfirmOrderSucceeded(transactionId);
                    }
                    else
                    {
                        ConfirmCallback?.OnConfirmOrderFailed(
                            new FailedOrder(pendingOrder, PurchaseFailureReason.Unknown,
                                result?.Error?.ToString() ?? "Failed to consume the purchase."));
                    }
                });
            });
        }

        #endregion

        #region Fetch purchases (restore)

        public override void FetchPurchases()
        {
            RunAsync(async () =>
            {
                var unfinished = await AppCoinsNativeBridge.GetUnfinishedPurchases();
                var all = await AppCoinsNativeBridge.GetAllPurchases();

                Dispatch(() =>
                {
                    bool unfinishedOk = unfinished != null && unfinished.IsSuccess;
                    bool allOk = all != null && all.IsSuccess;

                    if (!unfinishedOk && !allOk)
                    {
                        PurchaseFetchCallback?.OnPurchasesRetrievalFailed(
                            new PurchasesFetchFailureDescription(
                                PurchasesFetchFailureReason.Unknown,
                                unfinished?.Error?.ToString() ?? all?.Error?.ToString() ?? "Failed to fetch purchases."));
                        return;
                    }

                    var orders = new List<Order>();
                    var seen = new HashSet<string>();

                    AppendOrders(orders, seen, unfinished?.Value);
                    AppendOrders(orders, seen, all?.Value);

                    PurchaseFetchCallback?.OnAllPurchasesRetrieved(orders);
                });
            });
        }

        private void AppendOrders(List<Order> orders, HashSet<string> seen, ACPurchase[] purchases)
        {
            if (purchases == null) return;

            foreach (var p in purchases)
            {
                if (p == null || string.IsNullOrEmpty(p.UID) || !seen.Add(p.UID))
                {
                    continue;
                }

                var cart = BuildCart(p.Sku);
                if (cart == null)
                {
                    // Product not in the cache (not fetched yet) - can't build a cart item.
                    continue;
                }

                var receipt = AppCoinsReceipt.Build(p, null);
                var info = new AppCoinsOrderInfo(receipt, p.OrderUID);

                if (IsUnfinished(p.State))
                {
                    orders.Add(new PendingOrder(cart, info));
                }
                else
                {
                    orders.Add(new ConfirmedOrder(cart, info));
                }
            }
        }

        #endregion

        #region Check entitlement

        public override void CheckEntitlement(ProductDefinition product)
        {
            RunAsync(async () =>
            {
                var result = await AppCoinsNativeBridge.GetLatestPurchase(product.storeSpecificId);
                Dispatch(() =>
                {
                    var status = EntitlementStatus.NotEntitled;
                    string message = null;

                    if (result == null || !result.IsSuccess)
                    {
                        status = EntitlementStatus.Unknown;
                        message = result?.Error?.ToString();
                    }
                    else if (result.Value != null && IsUnfinished(result.Value.State))
                    {
                        // A consumable that was paid for but not yet consumed.
                        status = EntitlementStatus.EntitledUntilConsumed;
                    }

                    EntitlementCallback?.OnCheckEntitlement(product, status, message);
                });
            });
        }

        #endregion

        #region Helpers

        internal ICart BuildCart(string storeSpecificId)
        {
            if (string.IsNullOrEmpty(storeSpecificId)) return null;
            var product = ReadOnlyProductCache?.FindByStoreSpecificId(storeSpecificId);
            return product == null ? null : new Cart(new CartItem(product));
        }

        private static string FirstStoreSpecificId(ICart cart)
        {
            var item = cart?.Items()?.FirstOrDefault();
            return item?.Product?.definition?.storeSpecificId;
        }

        private static bool IsUnfinished(string state)
        {
            return state == AppCoinsNativeBridge.PURCHASE_PENDING
                || state == AppCoinsNativeBridge.PURCHASE_ACKNOWLEDGED;
        }

        private static decimal ParsePrice(string priceValue)
        {
            return decimal.TryParse(priceValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var price)
                ? price
                : 0m;
        }

        private static (ProductFetchFailureReason, string) MapProductFetchFailure(AppCoinsSDKError error)
        {
            if (error == null)
            {
                return (ProductFetchFailureReason.Unknown, "Failed to fetch products.");
            }

            switch (error.Type)
            {
                case "productUnavailable":
                    return (ProductFetchFailureReason.ProductsUnavailable, error.ToString());
                case "networkError":
                    return (ProductFetchFailureReason.ProviderUnavailable, error.ToString());
                default:
                    return (ProductFetchFailureReason.Unknown, error.ToString());
            }
        }

        private static (PurchaseFailureReason, string) MapPurchaseFailure(AppCoinsSDKError error)
        {
            if (error == null)
            {
                return (PurchaseFailureReason.Unknown, "Purchase failed.");
            }

            switch (error.Type)
            {
                case "productUnavailable":
                    return (PurchaseFailureReason.ProductUnavailable, error.ToString());
                case "purchaseNotAllowed":
                case "notEntitled":
                    return (PurchaseFailureReason.PaymentDeclined, error.ToString());
                default:
                    return (PurchaseFailureReason.Unknown, error.ToString());
            }
        }

        private static void Dispatch(Action action)
        {
            UnityMainThreadDispatcher.Enqueue(action);
        }

        private static async void RunAsync(Func<Task> operation)
        {
            try
            {
                await operation();
            }
            catch (Exception e)
            {
                Debug.LogError("[AppCoins] Store operation failed: " + e);
            }
        }

        #endregion
    }
}
