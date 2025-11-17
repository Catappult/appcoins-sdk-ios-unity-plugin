using System.Runtime.InteropServices;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

[Serializable]
public class AppCoinsSDKError {
    public string Type;
    public string Message;
    public string Description;
    public ErrorRequest Request;

    [Serializable]
    public class ErrorRequest {
        public string URL;
        public string Method;
        public string Body;
        public string ResponseData;
        public int StatusCode;
    }

    public override string ToString() {
        if (Request.URL != null) {
            return $@"""{{
    ""type"": ""{Type}"",
    ""message"": ""{Message}"",
    ""description"": ""{Description}"",
    ""request"": {{
        ""url"": ""{Request.URL}"",
        ""method"": ""{Request.Method}"",
        ""body"": ""{Request.Body}"",
        ""responseData"": ""{Request.ResponseData}"",
        ""statusCode"": {Request.StatusCode}
    }}
}}""";
        }
        else {
            return $@"""{{
    ""type"": ""{Type}"",
    ""message"": ""{Message}"",
    ""description"": ""{Description}""
}}""";
        }
    }
}

[Serializable]
public class AppCoinsSDKResult<T> {
    public bool IsSuccess;
    public T Value;
    public AppCoinsSDKError Error;

    public static AppCoinsSDKResult<T> Success(T value) {
        return new AppCoinsSDKResult<T> {
            IsSuccess = true,
            Value     = value,
            Error     = null
        };
    }

    public static AppCoinsSDKResult<T> Failure(string type, string message, string description) {
        return new AppCoinsSDKResult<T> {
            IsSuccess = false,
            Error     = new AppCoinsSDKError {
                Type               = type,
                Message            = message,
                Description        = description
            }
        };
    }
}

[Serializable]
public class AppCoinsSDKPurchaseResult {
    public string State;
    public PurchaseValue Value;
    public AppCoinsSDKError Error;

    [Serializable]
    public class PurchaseValue {
        public string VerificationResult;
        public Purchase Purchase;
        public AppCoinsSDKError VerificationError;
    }

    public static AppCoinsSDKPurchaseResult Success(Purchase purchase, string verificationResult, AppCoinsSDKError verificationError = null) {
        return new AppCoinsSDKPurchaseResult {
            State = AppCoinsSDK.PURCHASE_STATE_SUCCESS,
            Value  = new PurchaseValue {
                Purchase           = purchase,
                VerificationResult = verificationResult,
                VerificationError  = verificationError
            },
            Error  = null
        };
    }

    public static AppCoinsSDKPurchaseResult Pending() {
        return new AppCoinsSDKPurchaseResult {
            State = AppCoinsSDK.PURCHASE_STATE_PENDING,
            Value  = null,
            Error  = null
        };
    }

    public static AppCoinsSDKPurchaseResult Cancelled() {
        return new AppCoinsSDKPurchaseResult {
            State = AppCoinsSDK.PURCHASE_STATE_USER_CANCELLED,
            Value  = null,
            Error  = null
        };
    }

    public static AppCoinsSDKPurchaseResult Failure(string type, string message, string description) {
        return new AppCoinsSDKPurchaseResult {
            State = AppCoinsSDK.PURCHASE_STATE_FAILED,
            Value  = null,
            Error  = new AppCoinsSDKError {
                Type               = type,
                Message            = message,
                Description        = description
            }
        };
    }
}

[Serializable]
public class Product
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
public class Purchase
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
public class PurchaseIntent
{
    public string ID;
    public Product Product;
    public string Timestamp;
}

public class AppCoinsSDK
{
    public const string PURCHASE_PENDING = "pending";
    public const string PURCHASE_ACKNOWLEDGED = "acknowledged";
    public const string PURCHASE_CONSUMED = "consumed";

    public const string PURCHASE_STATE_SUCCESS = "success";
    public const string PURCHASE_STATE_PENDING = "pending";
    public const string PURCHASE_STATE_USER_CANCELLED = "userCancelled";
    public const string PURCHASE_STATE_FAILED = "failed";

    public const string PURCHASE_VERIFICATION_STATE_VERIFIED = "verified";
    public const string PURCHASE_VERIFICATION_STATE_UNVERIFIED = "unverified";

    private static AppCoinsSDK _instance;
    private static readonly object _lock = new object();

    private delegate void JsonCallback(string result);

    private static bool _isObservingPurchases = false;

    [DllImport("__Internal")]
    private static extern void _initialize();

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
    private static extern IntPtr _getTestingWalletAddress(JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getPurchaseIntent(JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _confirmPurchaseIntent(string payload, JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _rejectPurchaseIntent();

    [DllImport("__Internal")]
    private static extern void _startPurchaseUpdates();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSDK()
    {
        var _ = AppCoinsSDK.Instance;
    }

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

                        _initialize();

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

    #region Initialize
    public void Initialize()
    {
        _initialize();
    }
    #endregion

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
        var result = JsonUtility.FromJson<AppCoinsSDKResult<bool>>(json);
        Instance._tcsIsAvailable.SetResult(result.IsSuccess);
    }
#endif

    #endregion

    #region Get Products
    private TaskCompletionSource<AppCoinsSDKResult<Product[]>> _tcsGetProducts;
    public async Task<AppCoinsSDKResult<Product[]>> GetProducts(string[] skus = null)
    {
#if UNITY_IOS && !UNITY_EDITOR
        skus ??= Array.Empty<string>();
        this._tcsGetProducts = new TaskCompletionSource<AppCoinsSDKResult<Product[]>>();
        _getProducts(skus, skus.Length, OnGetProductsCompleted);
        return await this._tcsGetProducts.Task;        
#else
        return await Task.FromResult(
            AppCoinsSDKResult<Product[]>.Failure(
                type:        "systemError",
                message:     "AppCoins SDK unavailable",
                description: "GetProducts() called on unsupported platform at AppCoinsSDK.cs:GetProducts"
            )
        );
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnGetProductsCompleted(string json)
    {
        var result = JsonUtility.FromJson<AppCoinsSDKResult<Product[]>>(json);
        Instance._tcsGetProducts.SetResult(result);
    }
#endif

    #endregion

    #region Purchase Product
    private TaskCompletionSource<AppCoinsSDKPurchaseResult> _tcsPurchase;
    public async Task<AppCoinsSDKPurchaseResult> Purchase(string sku, string payload = "")
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsPurchase = new TaskCompletionSource<AppCoinsSDKPurchaseResult>();
        _purchase(sku, payload, OnPurchaseCompleted);
        return await this._tcsPurchase.Task;        
#else
        return await Task.FromResult(
            AppCoinsSDKPurchaseResult.Failure(
                type:        "systemError",
                message:     "AppCoins SDK unavailable",
                description: "Purchase() called on unsupported platform at AppCoinsSDK.cs:Purchase"
            )
        );
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnPurchaseCompleted(string json)
    {
        var result = JsonUtility.FromJson<AppCoinsSDKPurchaseResult>(json);
        Instance._tcsPurchase.SetResult(result);
    }
#endif

    #endregion

    #region Get All Purchases
    private TaskCompletionSource<AppCoinsSDKResult<Purchase[]>> _tcsGetAllPurchases;
    public async Task<AppCoinsSDKResult<Purchase[]>> GetAllPurchases()
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsGetAllPurchases = new TaskCompletionSource<AppCoinsSDKResult<Purchase[]>>();
        _getAllPurchases(OnGetAllPurchasesCompleted);
        return await this._tcsGetAllPurchases.Task;        
#else
        return await Task.FromResult(
            AppCoinsSDKResult<Purchase[]>.Failure(
                type:        "systemError",
                message:     "AppCoins SDK unavailable",
                description: "GetAllPurchases() called on unsupported platform at AppCoinsSDK.cs:GetAllPurchases"
            )
        );
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnGetAllPurchasesCompleted(string json)
    {
        var result = JsonUtility.FromJson<AppCoinsSDKResult<Purchase[]>>(json);
        Instance._tcsGetAllPurchases.SetResult(result);
    }
#endif

    #endregion

    #region Get Latest Purchase
    private TaskCompletionSource<AppCoinsSDKResult<Purchase>> _tcsGetLatestPurchase;
    public async Task<AppCoinsSDKResult<Purchase>> GetLatestPurchase(string sku)
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsGetLatestPurchase = new TaskCompletionSource<AppCoinsSDKResult<Purchase>>();
        _getLatestPurchase(sku, OnGetLatestPurchaseCompleted);
        return await this._tcsGetLatestPurchase.Task;        
#else
        return await Task.FromResult(
            AppCoinsSDKResult<Purchase>.Failure(
                type:        "systemError",
                message:     "AppCoins SDK unavailable",
                description: "GetLatestPurchase() called on unsupported platform at AppCoinsSDK.cs:GetLatestPurchase"
            )
        );
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnGetLatestPurchaseCompleted(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) {
            Instance._tcsGetLatestPurchase.SetResult(AppCoinsSDKResult<Purchase>.Success(null));
            return;
        }

        var dict = MiniJsonAppCoins.Deserialize(json) as Dictionary<string, object>;

        if (dict == null || !dict.ContainsKey("Value") || dict["Value"] == null) {
            Instance._tcsGetLatestPurchase.SetResult(AppCoinsSDKResult<Purchase>.Success(null));
            return;
        }
        
        var result = JsonUtility.FromJson<AppCoinsSDKResult<Purchase>>(json);
        Instance._tcsGetLatestPurchase.SetResult(result);
    }
#endif

    #endregion

    #region Get Unfinished Purchases
    private TaskCompletionSource<AppCoinsSDKResult<Purchase[]>> _tcsGetUnfinishedPurchases;
    public async Task<AppCoinsSDKResult<Purchase[]>> GetUnfinishedPurchases()
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsGetUnfinishedPurchases = new TaskCompletionSource<AppCoinsSDKResult<Purchase[]>>();
        _getUnfinishedPurchases(OnGetUnfinishedPurchasesCompleted);
        return await this._tcsGetUnfinishedPurchases.Task;        
#else
        return await Task.FromResult(
            AppCoinsSDKResult<Purchase[]>.Failure(
                type:        "systemError",
                message:     "AppCoins SDK unavailable",
                description: "GetUnfinishedPurchases() called on unsupported platform at AppCoinsSDK.cs:GetUnfinishedPurchases"
            )
        );
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnGetUnfinishedPurchasesCompleted(string json)
    {
        var result = JsonUtility.FromJson<AppCoinsSDKResult<Purchase[]>>(json);
        Instance._tcsGetUnfinishedPurchases.SetResult(result);
    }
#endif

    #endregion

    #region Consume Purchase
    private TaskCompletionSource<AppCoinsSDKResult<bool>> _tcsConsumePurchase;
    public async Task<AppCoinsSDKResult<bool>> ConsumePurchase(string sku)
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsConsumePurchase = new TaskCompletionSource<AppCoinsSDKResult<bool>>();
        _consumePurchase(sku, OnConsumePurchaseCompleted);
        return await this._tcsConsumePurchase.Task;        
#else
        return await Task.FromResult(
            AppCoinsSDKResult<bool>.Failure(
                type:        "systemError",
                message:     "AppCoins SDK unavailable",
                description: "ConsumePurchase() called on unsupported platform at AppCoinsSDK.cs:ConsumePurchase"
            )
        );
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnConsumePurchaseCompleted(string json)
    {
        var result = JsonUtility.FromJson<AppCoinsSDKResult<bool>>(json);
        Instance._tcsConsumePurchase.SetResult(result);
    }
#endif

    #endregion

    #region Get Testing Wallet Address

    private TaskCompletionSource<AppCoinsSDKResult<string>> _tcsGetTestingWalletAddress;

    public async Task<AppCoinsSDKResult<string>> GetTestingWalletAddress()
    {
#if UNITY_IOS && !UNITY_EDITOR
        _tcsGetTestingWalletAddress = new TaskCompletionSource<AppCoinsSDKResult<string>>();
        _getTestingWalletAddress(OnGetTestingWalletAddressCompleted);
        return await this._tcsGetTestingWalletAddress.Task;
#else
        return await Task.FromResult(
            AppCoinsSDKResult<string>.Failure(
                type:        "systemError",
                message:     "AppCoins SDK unavailable",
                description: "GetTestingWalletAddress() called on unsupported platform at AppCoinsSDK.cs:GetTestingWalletAddress"
            )
        );
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnGetTestingWalletAddressCompleted(string json)
    {
        var result = JsonUtility.FromJson<AppCoinsSDKResult<string>>(json);
        Instance._tcsGetTestingWalletAddress.SetResult(result);
    }
#endif

    #endregion

    #region Get Purchase Intent
    private TaskCompletionSource<AppCoinsSDKResult<PurchaseIntent>> _tcsGetPurchaseIntent;
    public async Task<AppCoinsSDKResult<PurchaseIntent>> GetPurchaseIntent()
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsGetPurchaseIntent = new TaskCompletionSource<AppCoinsSDKResult<PurchaseIntent>>();
        _getPurchaseIntent(OnGetPurchaseIntentCompleted);
        return await this._tcsGetPurchaseIntent.Task;        
#else
        return await Task.FromResult(
            AppCoinsSDKResult<PurchaseIntent>.Failure(
                type:        "systemError",
                message:     "AppCoins SDK unavailable",
                description: "GetPurchaseIntent() called on unsupported platform at AppCoinsSDK.cs:GetPurchaseIntent"
            )
        );
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnGetPurchaseIntentCompleted(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) {
            Instance._tcsGetPurchaseIntent.SetResult(AppCoinsSDKResult<PurchaseIntent>.Success(null));
            return;
        }

        var dict = MiniJsonAppCoins.Deserialize(json) as Dictionary<string, object>;

        if (dict == null || !dict.ContainsKey("Value") || dict["Value"] == null) {
            Instance._tcsGetPurchaseIntent.SetResult(AppCoinsSDKResult<PurchaseIntent>.Success(null));
            return;
        }

        var result = JsonUtility.FromJson<AppCoinsSDKResult<PurchaseIntent>>(json);
        Instance._tcsGetPurchaseIntent.SetResult(result);
    }
#endif

    #endregion

    #region Confirm Purchase Intent
    private TaskCompletionSource<AppCoinsSDKPurchaseResult> _tcsConfirmPurchaseIntent;
    public async Task<AppCoinsSDKPurchaseResult> ConfirmPurchaseIntent(string payload = "")
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsConfirmPurchaseIntent = new TaskCompletionSource<AppCoinsSDKPurchaseResult>();
        _confirmPurchaseIntent(payload, OnConfirmPurchaseIntentCompleted);
        return await this._tcsConfirmPurchaseIntent.Task;        
#else
        return await Task.FromResult(
            AppCoinsSDKPurchaseResult.Failure(
                type:        "systemError",
                message:     "AppCoins SDK unavailable",
                description: "ConfirmPurchaseIntent() called on unsupported platform at AppCoinsSDK.cs:ConfirmPurchaseIntent"
            )
        );
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnConfirmPurchaseIntentCompleted(string json)
    {
        var result = JsonUtility.FromJson<AppCoinsSDKPurchaseResult>(json);
        Instance._tcsConfirmPurchaseIntent.SetResult(result);
    }
#endif

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
