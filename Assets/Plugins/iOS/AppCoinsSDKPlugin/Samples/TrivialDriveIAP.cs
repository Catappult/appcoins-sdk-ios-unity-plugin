using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using AppCoins.Unity;

/// <summary>
/// Trivial Drive sample using the standard Unity IAP v5 API with AppCoins as
/// the backing store on iOS alternative distribution.
///
/// The only AppCoins-specific line is the one-time
/// <see cref="AppCoinsIAP.ConfigureStoreAsync"/> call. Everything else is
/// plain Unity IAP, so the same code works when the game ships through the
/// Apple App Store.
/// </summary>
public class TrivialDriveIAP : MonoBehaviour
{
    // Catappult In-App Product SKUs.
    private static readonly string[] Skus = { "gas", "coins_100" };

    private StoreController _controller;

    private async void Start()
    {
        // 1) Pick the backing store. Automatic uses the AppCoins isAvailable
        //    check to choose AppCoins Billing when available, otherwise Apple.
        string store = await AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic);
        Debug.Log($"[TrivialDrive] Active store: {store}");

        // 2) Create a controller for the default store and subscribe to events.
        _controller = UnityIAPServices.StoreController();
        _controller.OnStoreConnected += OnConnected;
        _controller.OnStoreDisconnected += desc =>
            Debug.LogWarning("[TrivialDrive] Store disconnected: " + desc.Message);
        _controller.OnProductsFetched += OnProductsFetched;
        _controller.OnProductsFetchFailed += f =>
            Debug.LogError("[TrivialDrive] Products fetch failed: " + f.FailureReason);
        _controller.OnPurchasePending += OnPurchasePending;
        _controller.OnPurchaseConfirmed += OnPurchaseConfirmed;
        _controller.OnPurchaseDeferred += _ =>
            Debug.Log("[TrivialDrive] Purchase deferred (payment pending).");
        _controller.OnPurchaseFailed += f =>
            Debug.LogError($"[TrivialDrive] Purchase failed: {f.FailureReason} - {f.Details}");

        // 3) Connect to the store.
        await _controller.Connect();
    }

    private void OnConnected()
    {
        Debug.Log("[TrivialDrive] Store connected. Fetching products...");

        var definitions = new List<ProductDefinition>();
        foreach (var sku in Skus)
        {
            definitions.Add(new ProductDefinition(sku, ProductType.Consumable));
        }
        _controller.FetchProducts(definitions);

        // Recover any purchases that were paid for but not yet consumed
        // (unfinished-purchase recovery via the standard Unity IAP flow).
        _controller.FetchPurchases();
    }

    private void OnProductsFetched(List<Product> products)
    {
        foreach (var product in products)
        {
            Debug.Log($"[TrivialDrive] {product.definition.id}: " +
                      $"{product.metadata.localizedTitle} @ {product.metadata.localizedPriceString}");
        }
    }

    /// <summary>Hook this up to a "Buy" button.</summary>
    public void Buy(string sku)
    {
        var product = _controller?.GetProductById(sku);
        if (product != null)
        {
            _controller.PurchaseProduct(product);
        }
    }

    private void OnPurchasePending(PendingOrder order)
    {
        // order.Info.Receipt carries the AppCoins signature verification, which
        // you can forward to your backend to server-validate (Remote Check).
        Debug.Log("[TrivialDrive] Purchase pending. Receipt: " + order.Info.Receipt);

        GrantEntitlement(order);

        // Confirm the order to consume it. On AppCoins this maps to Purchase.finish.
        _controller.ConfirmPurchase(order);
    }

    private void OnPurchaseConfirmed(Order order)
    {
        Debug.Log("[TrivialDrive] Purchase confirmed: " + order.Info.TransactionID);
    }

    private void GrantEntitlement(Order order)
    {
        foreach (var item in order.CartOrdered.Items())
        {
            Debug.Log($"[TrivialDrive] Granting {item.Product.definition.id} x{item.Quantity}");
        }
    }
}
