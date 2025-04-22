using System.Runtime.InteropServices;
using UnityEngine;
using System.Threading.Tasks;
using System;

[Serializable]
public class IsAvailableResponse
{
    public bool Available;
}

[Serializable]
public class ProductData
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
public class GetProductsResponse
{
    public ProductData[] Products;
}

[Serializable]
public class PurchaseData
{
    public string UID;
    public string Sku;
    public string State;
    public string OrderUID;
    public string Payload;
    public string Created;
    public PurchaseVerification Verification;

    [Serializable]
    public class PurchaseVerification
    {
        public string Type;
        public string Signature;
        public PurchaseVerificationData Data;
    }

    [Serializable]
    public class PurchaseVerificationData
    {
        public string OrderId;
        public string PackageName;
        public string ProductId;
        public int PurchaseTime;
        public string PurchaseToken;
        public int PurchaseState;
        public string DeveloperPayload;
    }
}

[Serializable]
public class GetPurchasesResponse
{
    public PurchaseData[] Purchases;
}

[Serializable]
public class PurchaseResponse
{
    public string State;
    public string Error;
    public PurchaseData Purchase;
}

[Serializable]
public class ConsumePurchaseResponse
{
    public bool Success;
    public string Error;
}

[Serializable]
public class PurchaseIntentData
{
    public string ID;
    public ProductData Product;
    public string Timestamp;
}

public class AppCoinsSDK
{
    public const string PURCHASE_PENDING = "PENDING";
    public const string PURCHASE_ACKNOWLEDGED = "ACKNOWLEDGED";
    public const string PURCHASE_CONSUMED = "CONSUMED";

    public const string PURCHASE_STATE_SUCCESS = "success";
    public const string PURCHASE_STATE_UNVERIFIED = "unverified";
    public const string PURCHASE_STATE_USER_CANCELLED = "user_cancelled";
    public const string PURCHASE_STATE_FAILED = "failed";

    private static AppCoinsSDK _instance;
    private static readonly object _lock = new object();

    private delegate void JsonCallback(string result);

    private static bool _isObservingPurchases = false;

    [DllImport("__Internal")]
    private static extern void _handleDeepLink(string url, JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _isAvailable(JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getProducts(string[] skus, int count, JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _purchase(string sku, string payload, JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getAllPurchases(JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getLatestPurchase(string sku, JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getUnfinishedPurchases(JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _consumePurchase(string sku, JsonCallback callback);

    [DllImport("__Internal")]
    private static extern IntPtr _getTestingWalletAddress();

    [DllImport("__Internal")]
    private static extern void _getPurchaseIntent(JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _confirmPurchaseIntent(string payload, JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _rejectPurchaseIntent();

    [DllImport("__Internal")]
    private static extern void _startPurchaseUpdates();

    public static AppCoinsSDK Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock) // Thread-safe singleton
                {
                    if (_instance == null)
                    {
                        _instance = new AppCoinsSDK();

                        // Check for cold-start deep link (only if iOS)
                        if (!string.IsNullOrEmpty(Application.absoluteURL))
                        {
                            #if UNITY_IOS && !UNITY_EDITOR
                            _handleDeepLink(Application.absoluteURL, HandleDeepLinkResponse);
                            #endif
                        }

                        Application.deepLinkActivated += HandleDeepLinkActivated;
                    }
                }
            }
            return _instance;
        }
    }

    ~AppCoinsSDK()
    {
        Application.deepLinkActivated -= HandleDeepLinkActivated;
    }

    private static void HandleDeepLinkActivated(string url)
    {
        _handleDeepLink(url, HandleDeepLinkResponse);
        Debug.Log("Deep link activated: " + url);
    }

    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void HandleDeepLinkResponse(string json)
    {
        Debug.Log("Deep link response: " + json);
    }

    #region Is Available
    private TaskCompletionSource<bool> _tcsIsAvailable;
    public async Task<bool> IsAvailable()
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsIsAvailable = new TaskCompletionSource<bool>();
        _isAvailable(OnAvailabilityCheckCompleted);
        return await this._tcsIsAvailable.Task;        
#else
        Debug.Log("AppCoins SDK is not available.");
        return await Task.FromResult(false);
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnAvailabilityCheckCompleted(string json)
    {
        var response = JsonUtility.FromJson<IsAvailableResponse>(json);
        Instance._tcsIsAvailable.SetResult(response.Available);
    }
#endif

    #endregion

    #region Get Products
    private TaskCompletionSource<ProductData[]> _tcsGetProducts;
    public async Task<ProductData[]> GetProducts(string[] skus = null)
    {
#if UNITY_IOS && !UNITY_EDITOR
        skus ??= new string[0];
        this._tcsGetProducts = new TaskCompletionSource<ProductData[]>();
        _getProducts(skus, skus.Length, OnGetProductsCompleted);
        return await this._tcsGetProducts.Task;        
#else
        return await Task.FromResult(new ProductData[0]);
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnGetProductsCompleted(string json)
    {
        var response = JsonUtility.FromJson<GetProductsResponse>("{\"Products\":" + json + "}");
        Instance._tcsGetProducts.SetResult(response.Products);
    }
#endif

    #endregion

    #region Purchase Product
    private TaskCompletionSource<PurchaseResponse> _tcsPurchase;
    public async Task<PurchaseResponse> Purchase(string sku, string payload = "")
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsPurchase = new TaskCompletionSource<PurchaseResponse>();
        _purchase(sku, payload, OnPurchaseCompleted);
        return await this._tcsPurchase.Task;        
#else
        return await Task.FromResult(new PurchaseResponse
        {
            State = "error",
            Error = "AppCoins SDK is not available."
        });
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnPurchaseCompleted(string json)
    {
        try
        {
            var response = JsonUtility.FromJson<PurchaseResponse>(json);
            Instance._tcsPurchase?.TrySetResult(response); // Use TrySetResult to avoid exception if already set
        }
        catch (Exception ex)
        {
            var fallback = new PurchaseResponse
            {
                State = "failed",
                Error = ex.ToString(),
                Purchase = new PurchaseData()
            };
            Instance._tcsPurchase?.TrySetResult(fallback);
        }
    }
#endif

    #endregion

    #region Get All Purchases
    private TaskCompletionSource<PurchaseData[]> _tcsGetAllPurchases;
    public async Task<PurchaseData[]> GetAllPurchases()
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsGetAllPurchases = new TaskCompletionSource<PurchaseData[]>();
        _getAllPurchases(OnGetAllPurchasesCompleted);
        return await this._tcsGetAllPurchases.Task;        
#else
        return await Task.FromResult(new PurchaseData[0]);
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnGetAllPurchasesCompleted(string json)
    {
        var response = JsonUtility.FromJson<GetPurchasesResponse>("{\"Purchases\":" + json + "}");
        Instance._tcsGetAllPurchases.SetResult(response.Purchases);
    }
#endif

    #endregion

    #region Get Latest Purchase
    private TaskCompletionSource<PurchaseData> _tcsGetLatestPurchase;
    public async Task<PurchaseData> GetLatestPurchase(string sku)
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsGetLatestPurchase = new TaskCompletionSource<PurchaseData>();
        _getLatestPurchase(sku, OnGetLatestPurchaseCompleted);
        return await this._tcsGetLatestPurchase.Task;        
#else
        return await Task.FromResult(new PurchaseData());
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnGetLatestPurchaseCompleted(string json)
    {
        var response = JsonUtility.FromJson<PurchaseData>(json);
        Instance._tcsGetLatestPurchase.SetResult(response);
    }
#endif

    #endregion

    #region Get Unfinished Purchases
    private TaskCompletionSource<PurchaseData[]> _tcsGetUnfinishedPurchases;
    public async Task<PurchaseData[]> GetUnfinishedPurchases()
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsGetUnfinishedPurchases = new TaskCompletionSource<PurchaseData[]>();
        _getUnfinishedPurchases(OnGetUnfinishedPurchasesCompleted);
        return await this._tcsGetUnfinishedPurchases.Task;        
#else
        return await Task.FromResult(new PurchaseData[0]);
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnGetUnfinishedPurchasesCompleted(string json)
    {
        var response = JsonUtility.FromJson<GetPurchasesResponse>("{\"Purchases\":" + json + "}");
        Instance._tcsGetUnfinishedPurchases.SetResult(response.Purchases);
    }
#endif

    #endregion

    #region Consume Purchase
    private TaskCompletionSource<ConsumePurchaseResponse> _tcsConsumePurchase;
    public async Task<ConsumePurchaseResponse> ConsumePurchase(string sku)
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsConsumePurchase = new TaskCompletionSource<ConsumePurchaseResponse>();
        _consumePurchase(sku, OnConsumePurchaseCompleted);
        return await this._tcsConsumePurchase.Task;        
#else
        return await Task.FromResult(new ConsumePurchaseResponse
        {
            Success = false,
            Error = "AppCoins SDK is not available."
        });
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnConsumePurchaseCompleted(string json)
    {
        var response = JsonUtility.FromJson<ConsumePurchaseResponse>(json);
        Instance._tcsConsumePurchase.SetResult(response);
    }
#endif

    #endregion

    #region Get Testing Wallet Address
    public string GetTestingWalletAddress()
    {
        IntPtr ptr = _getTestingWalletAddress();
        return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
    }
    #endregion

    #region Get Purchase Intent
    private TaskCompletionSource<PurchaseIntentData> _tcsGetPurchaseIntent;
    public async Task<PurchaseIntentData> GetPurchaseIntent()
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsGetPurchaseIntent = new TaskCompletionSource<PurchaseIntentData>();
        _getPurchaseIntent(OnGetPurchaseIntentCompleted);
        return await this._tcsGetPurchaseIntent.Task;        
#else
        return await Task.FromResult(new PurchaseIntentData());
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnGetPurchaseIntentCompleted(string json)
    {
        var response = JsonUtility.FromJson<PurchaseIntentData>(json);
        Instance._tcsGetPurchaseIntent.SetResult(response);
    }
#endif

    #endregion

    #region Confirm Purchase Intent
    private TaskCompletionSource<PurchaseResponse> _tcsConfirmPurchaseIntent;
    public async Task<PurchaseResponse> ConfirmPurchaseIntent(string payload = "")
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsConfirmPurchaseIntent = new TaskCompletionSource<PurchaseResponse>();
        _confirmPurchaseIntent(payload, OnPurchaseCompleted);
        return await this._tcsConfirmPurchaseIntent.Task;        
#else
        return await Task.FromResult(new PurchaseResponse
        {
            State = "error",
            Error = "AppCoins SDK is not available."
        });
#endif
    }

    #endregion

    #region Reject Purchase Intent
    public void RejectPurchaseIntent()
    {
        _rejectPurchaseIntent();
    }
    #endregion

    #region Start Observing Indirect Purchases
    public static void StartObservingPurchases()
    {
        if (_isObservingPurchases)
            return; // Prevent multiple registrations
        
        _isObservingPurchases = true;
        _startPurchaseUpdates();
    }
    #endregion

}


