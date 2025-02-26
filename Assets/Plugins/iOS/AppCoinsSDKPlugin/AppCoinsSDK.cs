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
public class AppCoinsPluginSDKError 
{
    public string errorType;
    public DebugInfo debugInfo;

    public override string ToString()
    {
        return $"ErrorType: {errorType},\nDebugInfo: {debugInfo.ToString()}";
    }

    [Serializable]
    public class DebugInfo
    {
        public string message;
        public string description;
        public DebugRequestInfo request;

        public override string ToString()
        {
            return $"\nMessage: {message},\nDescription: {description},\nRequest: {request.ToString()}\n";
        }
    }

    [Serializable]
    public class DebugRequestInfo
    {
        public string url;
        public string body;
        public string method;
        public string responseData;
        public string statusCode;

        public override string ToString()
        {
            return $"\nURL: {url},\nBody: {body},\nMethod: {method},\nResponseData: {responseData},\nStatusCode: {statusCode}";
        }
    }

    public static AppCoinsPluginSDKError CreateAppCoinsPluginSDKError(string errorType, string message, string description)
    {
        return new AppCoinsPluginSDKError
        {
            errorType = errorType,
            debugInfo = new DebugInfo
            {
                message = message,
                description = description,
                request = new DebugRequestInfo()
            }
        };
    }
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
    private delegate void JsonCallback(string result);
    private delegate void ResultHandlingJsonCallback(string jsonSuccess, string jsonError);

    [DllImport("__Internal")]
    private static extern void _handleDeepLink(string url, JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _isAvailable(JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getProducts(string[] skus, int count, ResultHandlingJsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _purchase(string sku, string payload, ResultHandlingJsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getAllPurchases(ResultHandlingJsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getLatestPurchase(string sku, ResultHandlingJsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getUnfinishedPurchases(ResultHandlingJsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _consumePurchase(string sku, ResultHandlingJsonCallback callback);

    [DllImport("__Internal")]
    private static extern IntPtr _getTestingWalletAddress();

    public static AppCoinsSDK Instance
    {
        get
        {
            _instance ??= new AppCoinsSDK();
            Application.deepLinkActivated += HandleDeepLinkActivated;
            return _instance;
        }
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
            skus ??= Array.Empty<string>();
            this._tcsGetProducts = new TaskCompletionSource<ProductData[]>();
            _getProducts(skus, skus.Length, OnGetProductsCompleted);
            return await this._tcsGetProducts.Task;
        #else
            throw new NotSupportedException("AppCoins SDK is not available at AppCoinsSDK.cs:GetProducts");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(ResultHandlingJsonCallback))]
    private static void OnGetProductsCompleted(string jsonSuccess, string jsonError)
    {
        try
        {
            if (!string.IsNullOrEmpty(jsonError))
            {
                var sdkError = JsonUtility.FromJson<AppCoinsPluginSDKError>(jsonError);
                if (sdkError != null)
                {
                    throw new Exception($"{sdkError}");
                }
                else
                {
                    throw new Exception("Failure in error JSON deserialization at AppCoinsSDK.cs:GetProducts");
                }
            }
            else if (!string.IsNullOrEmpty(jsonSuccess))
            {
                var response = JsonUtility.FromJson<GetProductsResponse>("{\"Products\":" + jsonSuccess + "}");
                if (response != null)
                {
                    Instance._tcsGetProducts.SetResult(response.Products);
                }
                else
                {
                    throw new Exception("Failure in success JSON deserialization at AppCoinsSDK.cs:GetProducts");
                }
            }
            else
            {
                throw new Exception("Both callback parameters are empty or null at AppCoinsSDK.cs:GetProducts");
            }
        }
        catch (Exception ex)
        {
            Instance._tcsGetProducts.SetException(ex);
        }
    }
    #endif
    #endregion

    #region Purchase
    private TaskCompletionSource<PurchaseResponse> _tcsPurchase;
    public async Task<PurchaseResponse> Purchase(string sku, string payload = "")
    {
        #if UNITY_IOS && !UNITY_EDITOR
            this._tcsPurchase = new TaskCompletionSource<PurchaseResponse>();
            _purchase(sku, payload, OnPurchaseCompleted);
            return await this._tcsPurchase.Task;        
        #else
            throw new NotSupportedException("AppCoins SDK is not available at AppCoinsSDK.cs:Purchase");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(ResultHandlingJsonCallback))]
    private static void OnPurchaseCompleted(string jsonSuccess, string jsonError)
    {
        try
        {
            if (!string.IsNullOrEmpty(jsonError))
            {
                var sdkError = JsonUtility.FromJson<AppCoinsPluginSDKError>(jsonError);
                if (sdkError != null)
                {
                    throw new Exception($"{sdkError}");
                }
                else
                {
                    throw new Exception("Failure in error JSON deserialization at AppCoinsSDK.cs:Purchase");
                }
            }
            else if (!string.IsNullOrEmpty(jsonSuccess))
            {
                var response = JsonUtility.FromJson<PurchaseResponse>(jsonSuccess);
                if (response != null)
                {
                    Instance._tcsPurchase.SetResult(response);
                }
                else
                {
                    throw new Exception("Failure in success JSON deserialization at AppCoinsSDK.cs:Purchase");
                }
            }
            else
            {
                throw new Exception("Both callback parameters are empty or null at AppCoinsSDK.cs:Purchase");
            }
        }
        catch (Exception ex)
        {
            Instance._tcsPurchase.SetException(ex);
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
            throw new NotSupportedException("AppCoins SDK is not available at AppCoinsSDK.cs:GetAllPurchases");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(ResultHandlingJsonCallback))]
    private static void OnGetAllPurchasesCompleted(string jsonSuccess, string jsonError)
    {
        try
        {
            if (!string.IsNullOrEmpty(jsonError))
            {
                var sdkError = JsonUtility.FromJson<AppCoinsPluginSDKError>(jsonError);
                if (sdkError != null)
                {
                    throw new Exception($"{sdkError}");
                }
                else
                {
                    throw new Exception("Failure in error JSON deserialization at AppCoinsSDK.cs:GetAllPurchases");
                }
            }
            else if (!string.IsNullOrEmpty(jsonSuccess))
            {
                var response = JsonUtility.FromJson<GetPurchasesResponse>("{\"Purchases\":" + jsonSuccess + "}");
                if (response != null)
                {
                    Instance._tcsGetAllPurchases.SetResult(response.Purchases);
                }
                else
                {
                    throw new Exception("Failure in success JSON deserialization at AppCoinsSDK.cs:GetAllPurchases");
                }
            }
            else
            {
                throw new Exception("Both callback parameters are empty or null at AppCoinsSDK.cs:GetAllPurchases");
            }
        }
        catch (Exception ex)
        {
            Instance._tcsGetAllPurchases.SetException(ex);
        }
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
            throw new NotSupportedException("AppCoins SDK is not available at AppCoinsSDK.cs:GetLatestPurchase");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(ResultHandlingJsonCallback))]
    private static void OnGetLatestPurchaseCompleted(string jsonSuccess, string jsonError)
    {
        try
        {
            if (!string.IsNullOrEmpty(jsonError))
            {
                var sdkError = JsonUtility.FromJson<AppCoinsPluginSDKError>(jsonError);
                if (sdkError != null)
                {
                    throw new Exception($"{sdkError}");
                }
                else
                {
                    throw new Exception("Failure in error JSON deserialization at AppCoinsSDK.cs:GetLatestPurchase");
                }
            }
            else if (!string.IsNullOrEmpty(jsonSuccess))
            {
                var response = JsonUtility.FromJson<PurchaseData>(jsonSuccess);
                if (response != null)
                {
                    Instance._tcsGetLatestPurchase.SetResult(response);
                }
                else
                {
                    throw new Exception("Failure in success JSON deserialization at AppCoinsSDK.cs:GetLatestPurchase");
                }
            }
            else
            {
                throw new Exception("Both callback parameters are empty or null at AppCoinsSDK.cs:GetLatestPurchase");
            }
        }
        catch (Exception ex)
        {
            Instance._tcsGetLatestPurchase.SetException(ex);
        }
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
            throw new NotSupportedException("AppCoins SDK is not available at AppCoinsSDK.cs:GetUnfinishedPurchases");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(ResultHandlingJsonCallback))]
    private static void OnGetUnfinishedPurchasesCompleted(string jsonSuccess, string jsonError)
    {
        try
        {
            if (!string.IsNullOrEmpty(jsonError))
            {
                var sdkError = JsonUtility.FromJson<AppCoinsPluginSDKError>(jsonError);
                if (sdkError != null)
                {
                    throw new Exception($"{sdkError}");
                }
                else
                {
                    throw new Exception("Failure in error JSON deserialization at AppCoinsSDK.cs:GetUnfinishedPurchases");
                }
            }
            else if (!string.IsNullOrEmpty(jsonSuccess))
            {
                var response = JsonUtility.FromJson<GetPurchasesResponse>("{\"Purchases\":" + jsonSuccess + "}");
                if (response != null)
                {
                    Instance._tcsGetUnfinishedPurchases.SetResult(response.Purchases);
                }
                else
                {
                    throw new Exception("Failure in success JSON deserialization at AppCoinsSDK.cs:GetUnfinishedPurchases");
                }
            }
            else
            {
                throw new Exception("Both callback parameters are empty or null at AppCoinsSDK.cs:GetUnfinishedPurchases");
            }
        }
        catch (Exception ex)
        {
            Instance._tcsGetUnfinishedPurchases.SetException(ex);
        }
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
            throw new NotSupportedException("AppCoins SDK is not available at AppCoinsSDK.cs:ConsumePurchase");
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(ResultHandlingJsonCallback))]
    private static void OnConsumePurchaseCompleted(string jsonSuccess, string jsonError)
    {
        try
        {
            if (!string.IsNullOrEmpty(jsonError))
            {
                var sdkError = JsonUtility.FromJson<AppCoinsPluginSDKError>(jsonError);
                if (sdkError != null)
                {
                    throw new Exception($"{sdkError}");
                }
                else
                {
                    throw new Exception("Failure in error JSON deserialization at AppCoinsSDK.cs:ConsumePurchase");
                }
            }
            else if (!string.IsNullOrEmpty(jsonSuccess))
            {
                var response = JsonUtility.FromJson<ConsumePurchaseResponse>(jsonSuccess);
                if (response != null)
                {
                    Instance._tcsConsumePurchase.SetResult(response);
                }
                else
                {
                    throw new Exception("Failure in success JSON deserialization at AppCoinsSDK.cs:ConsumePurchase");
                }
            }
            else
            {
                throw new Exception("Both callback parameters are empty or null at AppCoinsSDK.cs:ConsumePurchase");
            }
        }
        catch (Exception ex)
        {
            Instance._tcsConsumePurchase.SetException(ex);
        }
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

}
