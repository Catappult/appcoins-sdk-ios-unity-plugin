using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace AppCoins.Internal
{
    // ---------------------------------------------------------------------
    // Internal data models
    //
    // These types mirror the JSON produced by the native AppCoins bridge
    // (UnityPlugin.swift). They are deserialized with UnityEngine.JsonUtility
    // and are used exclusively by the Unity IAP v5 store adapter. They are
    // NOT part of the plugin's public API anymore — developers integrate
    // through Unity IAP (see AppCoins.Unity.AppCoinsIAP).
    // ---------------------------------------------------------------------

    [Serializable]
    internal class AppCoinsSDKError
    {
        public string Type;
        public string Message;
        public string Description;
        public ErrorRequest Request;

        [Serializable]
        internal class ErrorRequest
        {
            public string URL;
            public string Method;
            public string Body;
            public string ResponseData;
            public int StatusCode;
        }

        public override string ToString()
        {
            return $"{Type}: {Message} — {Description}";
        }
    }

    [Serializable]
    internal class AppCoinsSDKResult<T>
    {
        public bool IsSuccess;
        public T Value;
        public AppCoinsSDKError Error;

        public static AppCoinsSDKResult<T> Success(T value)
        {
            return new AppCoinsSDKResult<T> { IsSuccess = true, Value = value, Error = null };
        }

        public static AppCoinsSDKResult<T> Failure(string type, string message, string description)
        {
            return new AppCoinsSDKResult<T>
            {
                IsSuccess = false,
                Error = new AppCoinsSDKError { Type = type, Message = message, Description = description }
            };
        }
    }

    [Serializable]
    internal class AppCoinsSDKPurchaseResult
    {
        public string State;
        public PurchaseValue Value;
        public AppCoinsSDKError Error;

        [Serializable]
        internal class PurchaseValue
        {
            public string VerificationResult;
            public Purchase Purchase;
            public AppCoinsSDKError VerificationError;
        }

        public static AppCoinsSDKPurchaseResult Failure(string type, string message, string description)
        {
            return new AppCoinsSDKPurchaseResult
            {
                State = AppCoinsNativeBridge.PURCHASE_STATE_FAILED,
                Value = null,
                Error = new AppCoinsSDKError { Type = type, Message = message, Description = description }
            };
        }
    }

    [Serializable]
    internal class Product
    {
        public string Sku;
        public string Title;
        public string Description;
        public string PriceCurrency;
        public string PriceValue;
        public string PriceLabel;
        public string PriceSymbol;
    }

    [Serializable]
    internal class Purchase
    {
        public string UID;
        public string Sku;
        public string State;
        public string OrderUID;
        public string Payload;
        public string Created;
        public PurchaseVerification Verification;

        [Serializable]
        internal class PurchaseVerification
        {
            public string Type;
            public string Signature;
            public PurchaseVerificationData Data;
        }

        [Serializable]
        internal class PurchaseVerificationData
        {
            public string OrderId;
            public string PackageName;
            public string ProductId;
            public long PurchaseTime;
            public string PurchaseToken;
            public int PurchaseState;
            public string DeveloperPayload;
        }
    }

    [Serializable]
    internal class PurchaseIntent
    {
        public string ID;
        public Product Product;
        public string Timestamp;
    }

    // ---------------------------------------------------------------------
    // Internal native bridge
    //
    // Thin async wrappers over the Obj-C++/Swift P/Invoke layer
    // (UnityPluginBridge.mm / UnityPlugin.swift). Reused verbatim from the
    // former public AppCoinsSDK singleton, now marked internal.
    // ---------------------------------------------------------------------

    internal static class AppCoinsNativeBridge
    {
        // Native purchase states (must match UnityPlugin.swift)
        public const string PURCHASE_STATE_SUCCESS = "success";
        public const string PURCHASE_STATE_PENDING = "pending";
        public const string PURCHASE_STATE_USER_CANCELLED = "user_cancelled";
        public const string PURCHASE_STATE_FAILED = "failed";

        // Native purchase lifecycle states (Purchase.State)
        public const string PURCHASE_PENDING = "PENDING";
        public const string PURCHASE_ACKNOWLEDGED = "ACKNOWLEDGED";
        public const string PURCHASE_CONSUMED = "CONSUMED";

        // Verification results
        public const string PURCHASE_VERIFICATION_STATE_VERIFIED = "verified";
        public const string PURCHASE_VERIFICATION_STATE_UNVERIFIED = "unverified";

        private delegate void JsonCallback(string result);

        private static bool _initialized;
        private static bool _isObservingPurchases;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void _initialize();
        [DllImport("__Internal")] private static extern void _handleDeepLink(string url, JsonCallback callback);
        [DllImport("__Internal")] private static extern void _isAvailable(JsonCallback callback);
        [DllImport("__Internal")] private static extern void _getProducts(string[] skus, int count, JsonCallback callback);
        [DllImport("__Internal")] private static extern void _purchase(string sku, string payload, JsonCallback callback);
        [DllImport("__Internal")] private static extern void _getAllPurchases(JsonCallback callback);
        [DllImport("__Internal")] private static extern void _getLatestPurchase(string sku, JsonCallback callback);
        [DllImport("__Internal")] private static extern void _getUnfinishedPurchases(JsonCallback callback);
        [DllImport("__Internal")] private static extern void _consumePurchase(string sku, JsonCallback callback);
        [DllImport("__Internal")] private static extern void _getPurchaseIntent(JsonCallback callback);
        [DllImport("__Internal")] private static extern void _confirmPurchaseIntent(string payload, JsonCallback callback);
        [DllImport("__Internal")] private static extern void _rejectPurchaseIntent();
        [DllImport("__Internal")] private static extern void _startPurchaseUpdates();
#endif

        #region Initialize / deep links

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

#if UNITY_IOS && !UNITY_EDITOR
            _initialize();

            // Handle a cold-start deep link, if any.
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                _handleDeepLink(Application.absoluteURL, OnDeepLinkResponse);
            }

            Application.deepLinkActivated += OnDeepLinkActivated;
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        private static void OnDeepLinkActivated(string url)
        {
            _handleDeepLink(url, OnDeepLinkResponse);
        }

        [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
        private static void OnDeepLinkResponse(string json)
        {
            Debug.Log("[AppCoins] Deep link response: " + json);
        }
#endif

        #endregion

        #region Is Available

        private static TaskCompletionSource<bool> _tcsIsAvailable;

        public static Task<bool> IsAvailable()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _tcsIsAvailable = new TaskCompletionSource<bool>();
            _isAvailable(OnIsAvailable);
            return _tcsIsAvailable.Task;
#else
            return Task.FromResult(false);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
        private static void OnIsAvailable(string json)
        {
            var result = JsonUtility.FromJson<AppCoinsSDKResult<bool>>(json);
            _tcsIsAvailable.TrySetResult(result != null && result.IsSuccess);
        }
#endif

        #endregion

        #region Get Products

        private static TaskCompletionSource<AppCoinsSDKResult<Product[]>> _tcsGetProducts;

        public static Task<AppCoinsSDKResult<Product[]>> GetProducts(string[] skus = null)
        {
#if UNITY_IOS && !UNITY_EDITOR
            skus ??= Array.Empty<string>();
            _tcsGetProducts = new TaskCompletionSource<AppCoinsSDKResult<Product[]>>();
            _getProducts(skus, skus.Length, OnGetProducts);
            return _tcsGetProducts.Task;
#else
            return Task.FromResult(AppCoinsSDKResult<Product[]>.Failure(
                "systemError", "AppCoins SDK unavailable", "GetProducts() called on unsupported platform"));
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
        private static void OnGetProducts(string json)
        {
            _tcsGetProducts.TrySetResult(JsonUtility.FromJson<AppCoinsSDKResult<Product[]>>(json));
        }
#endif

        #endregion

        #region Purchase

        private static TaskCompletionSource<AppCoinsSDKPurchaseResult> _tcsPurchase;

        public static Task<AppCoinsSDKPurchaseResult> Purchase(string sku, string payload = "")
        {
#if UNITY_IOS && !UNITY_EDITOR
            _tcsPurchase = new TaskCompletionSource<AppCoinsSDKPurchaseResult>();
            _purchase(sku, payload ?? "", OnPurchase);
            return _tcsPurchase.Task;
#else
            return Task.FromResult(AppCoinsSDKPurchaseResult.Failure(
                "systemError", "AppCoins SDK unavailable", "Purchase() called on unsupported platform"));
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
        private static void OnPurchase(string json)
        {
            _tcsPurchase.TrySetResult(JsonUtility.FromJson<AppCoinsSDKPurchaseResult>(json));
        }
#endif

        #endregion

        #region Get All Purchases

        private static TaskCompletionSource<AppCoinsSDKResult<Purchase[]>> _tcsGetAllPurchases;

        public static Task<AppCoinsSDKResult<Purchase[]>> GetAllPurchases()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _tcsGetAllPurchases = new TaskCompletionSource<AppCoinsSDKResult<Purchase[]>>();
            _getAllPurchases(OnGetAllPurchases);
            return _tcsGetAllPurchases.Task;
#else
            return Task.FromResult(AppCoinsSDKResult<Purchase[]>.Failure(
                "systemError", "AppCoins SDK unavailable", "GetAllPurchases() called on unsupported platform"));
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
        private static void OnGetAllPurchases(string json)
        {
            _tcsGetAllPurchases.TrySetResult(JsonUtility.FromJson<AppCoinsSDKResult<Purchase[]>>(json));
        }
#endif

        #endregion

        #region Get Latest Purchase

        private static TaskCompletionSource<AppCoinsSDKResult<Purchase>> _tcsGetLatestPurchase;

        public static Task<AppCoinsSDKResult<Purchase>> GetLatestPurchase(string sku)
        {
#if UNITY_IOS && !UNITY_EDITOR
            _tcsGetLatestPurchase = new TaskCompletionSource<AppCoinsSDKResult<Purchase>>();
            _getLatestPurchase(sku, OnGetLatestPurchase);
            return _tcsGetLatestPurchase.Task;
#else
            return Task.FromResult(AppCoinsSDKResult<Purchase>.Failure(
                "systemError", "AppCoins SDK unavailable", "GetLatestPurchase() called on unsupported platform"));
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
        private static void OnGetLatestPurchase(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                _tcsGetLatestPurchase.TrySetResult(AppCoinsSDKResult<Purchase>.Success(null));
                return;
            }

            var dict = MiniJsonAppCoins.Deserialize(json) as Dictionary<string, object>;
            if (dict == null || !dict.ContainsKey("Value") || dict["Value"] == null)
            {
                _tcsGetLatestPurchase.TrySetResult(AppCoinsSDKResult<Purchase>.Success(null));
                return;
            }

            _tcsGetLatestPurchase.TrySetResult(JsonUtility.FromJson<AppCoinsSDKResult<Purchase>>(json));
        }
#endif

        #endregion

        #region Get Unfinished Purchases

        private static TaskCompletionSource<AppCoinsSDKResult<Purchase[]>> _tcsGetUnfinishedPurchases;

        public static Task<AppCoinsSDKResult<Purchase[]>> GetUnfinishedPurchases()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _tcsGetUnfinishedPurchases = new TaskCompletionSource<AppCoinsSDKResult<Purchase[]>>();
            _getUnfinishedPurchases(OnGetUnfinishedPurchases);
            return _tcsGetUnfinishedPurchases.Task;
#else
            return Task.FromResult(AppCoinsSDKResult<Purchase[]>.Failure(
                "systemError", "AppCoins SDK unavailable", "GetUnfinishedPurchases() called on unsupported platform"));
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
        private static void OnGetUnfinishedPurchases(string json)
        {
            _tcsGetUnfinishedPurchases.TrySetResult(JsonUtility.FromJson<AppCoinsSDKResult<Purchase[]>>(json));
        }
#endif

        #endregion

        #region Consume Purchase

        private static TaskCompletionSource<AppCoinsSDKResult<bool>> _tcsConsumePurchase;

        public static Task<AppCoinsSDKResult<bool>> ConsumePurchase(string sku)
        {
#if UNITY_IOS && !UNITY_EDITOR
            _tcsConsumePurchase = new TaskCompletionSource<AppCoinsSDKResult<bool>>();
            _consumePurchase(sku, OnConsumePurchase);
            return _tcsConsumePurchase.Task;
#else
            return Task.FromResult(AppCoinsSDKResult<bool>.Failure(
                "systemError", "AppCoins SDK unavailable", "ConsumePurchase() called on unsupported platform"));
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
        private static void OnConsumePurchase(string json)
        {
            _tcsConsumePurchase.TrySetResult(JsonUtility.FromJson<AppCoinsSDKResult<bool>>(json));
        }
#endif

        #endregion

        #region Confirm / Reject Purchase Intent

        private static TaskCompletionSource<AppCoinsSDKPurchaseResult> _tcsConfirmPurchaseIntent;

        public static Task<AppCoinsSDKPurchaseResult> ConfirmPurchaseIntent(string payload = "")
        {
#if UNITY_IOS && !UNITY_EDITOR
            _tcsConfirmPurchaseIntent = new TaskCompletionSource<AppCoinsSDKPurchaseResult>();
            _confirmPurchaseIntent(payload ?? "", OnConfirmPurchaseIntent);
            return _tcsConfirmPurchaseIntent.Task;
#else
            return Task.FromResult(AppCoinsSDKPurchaseResult.Failure(
                "systemError", "AppCoins SDK unavailable", "ConfirmPurchaseIntent() called on unsupported platform"));
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
        private static void OnConfirmPurchaseIntent(string json)
        {
            _tcsConfirmPurchaseIntent.TrySetResult(JsonUtility.FromJson<AppCoinsSDKPurchaseResult>(json));
        }
#endif

        public static void RejectPurchaseIntent()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _rejectPurchaseIntent();
#endif
        }

        #endregion

        #region Purchase updates (indirect / deep-link intents)

        public static void StartObservingPurchases()
        {
            if (_isObservingPurchases) return;
            _isObservingPurchases = true;
#if UNITY_IOS && !UNITY_EDITOR
            _startPurchaseUpdates();
#endif
        }

        #endregion
    }
}
