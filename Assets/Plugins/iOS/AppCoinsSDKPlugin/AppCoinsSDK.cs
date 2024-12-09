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

public class Result<TSuccess, TError>
{
    public TSuccess Success { get; }
    public TError Error { get; }
    public bool IsSuccess { get; }

    private Result(TSuccess success, TError error, bool isSuccess)
    {
        Success = success;
        Error = error;
        IsSuccess = isSuccess;
    }

    public static Result<TSuccess, TError> CreateSuccess(TSuccess success) =>
        new Result<TSuccess, TError>(success, default, true);

    public static Result<TSuccess, TError> CreateError(TError error) =>
        new Result<TSuccess, TError>(default, error, false);
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
    private TaskCompletionSource<Result<bool, AppCoinsPluginSDKError>> _tcsIsAvailable;
    public async Task<Result<bool, AppCoinsPluginSDKError>> IsAvailable()
    {
        #if UNITY_IOS && !UNITY_EDITOR
            this._tcsIsAvailable = new TaskCompletionSource<Result<bool, AppCoinsPluginSDKError>>();
            _isAvailable(OnAvailabilityCheckCompleted);
            return await this._tcsIsAvailable.Task;        
        #else
            return await Task.FromResult(Result<bool, AppCoinsPluginSDKError>.CreateError(
                AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                errorType: "Error", 
                message: "AppCoins SDK Availability", 
                description: "AppCoins SDK is not available at AppCoinsSDK.cs:IsAvailable")
            ));
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
        private static void OnAvailabilityCheckCompleted(string json)
        {
            var response = JsonUtility.FromJson<IsAvailableResponse>(json);
            if (response.Available == true) 
            {
                Instance._tcsIsAvailable.SetResult(Result<bool, AppCoinsPluginSDKError>.CreateSuccess(response.Available));
            } else {
                Instance._tcsIsAvailable.SetResult(Result<bool, AppCoinsPluginSDKError>.CreateError(
                    AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                    errorType: "Error", 
                    message: "AppCoins SDK Availability", 
                    description: "AppCoins SDK is not available at AppCoinsSDK.cs:IsAvailable")
                ));
            }
        }
    #endif

    #endregion

    #region Get Products

    private TaskCompletionSource<Result<ProductData[], AppCoinsPluginSDKError>> _tcsGetProducts;
    public async Task<Result<ProductData[], AppCoinsPluginSDKError>> GetProducts(string[] skus = null)
    {
        #if UNITY_IOS && !UNITY_EDITOR
            skus ??= new string[0];
            this._tcsGetProducts = new TaskCompletionSource<Result<ProductData[], AppCoinsPluginSDKError>>();
            _getProducts(skus, skus.Length, OnGetProductsCompleted);
            return await this._tcsGetProducts.Task;
        #else
            return await Task.FromResult(Result<ProductData[], AppCoinsPluginSDKError>.CreateError(
                AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                    errorType: "Error", 
                    message: "Failed to get products", 
                    description: "AppCoins SDK is not available at AppCoinsSDK.cs:GetProducts")
                ));
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(ResultHandlingJsonCallback))]
    private static void OnGetProductsCompleted(string jsonSuccess, string jsonError)
    {
        if (!string.IsNullOrEmpty(jsonError))
        {
            var sdkError = JsonUtility.FromJson<AppCoinsPluginSDKError>(jsonError);
            if (sdkError != null)
            {
                Instance._tcsGetProducts.SetResult(Result<ProductData[], AppCoinsPluginSDKError>.CreateError(sdkError));
            }
            else
            {
                Instance._tcsGetProducts.SetResult(Result<ProductData[], AppCoinsPluginSDKError>.CreateError(
                    AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                    errorType: "Error", 
                    message: "Failed to get products", 
                    description: "Failure in error JSON deserialization at AppCoinsSDK.cs:GetProducts")
                ));
            }
        }
        else if (!string.IsNullOrEmpty(jsonSuccess))
        {
            var response = JsonUtility.FromJson<GetProductsResponse>("{\"Products\":" + jsonSuccess + "}");
            if (response != null)
            {
                Instance._tcsGetProducts.SetResult(Result<ProductData[], AppCoinsPluginSDKError>.CreateSuccess(response.Products));
            }
            else
            {
                Instance._tcsGetProducts.SetResult(Result<ProductData[], AppCoinsPluginSDKError>.CreateError(
                    AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                    errorType: "Error", 
                    message: "Failed to get products", 
                    description: "Failure in success JSON deserialization at AppCoinsSDK.cs:GetProducts")
                ));
            }
        }
        else
        {
            Instance._tcsGetProducts.SetResult(Result<ProductData[], AppCoinsPluginSDKError>.CreateError(
                    AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                    errorType: "Error", 
                    message: "Failed to get products", 
                    description: "Both callback parameters are empty or null at AppCoinsSDK.cs:GetProducts")
                ));
        }
    }
    #endif
    #endregion

    #region Purchase
    
    private TaskCompletionSource<Result<PurchaseResponse, AppCoinsPluginSDKError>> _tcsPurchase;
    public async Task<Result<PurchaseResponse, AppCoinsPluginSDKError>> Purchase(string sku, string payload = "")
    {
        #if UNITY_IOS && !UNITY_EDITOR
                this._tcsPurchase = new TaskCompletionSource<Result<PurchaseResponse, AppCoinsPluginSDKError>>();
                _purchase(sku, payload, OnPurchaseCompleted);
                return await this._tcsPurchase.Task;        
        #else
                return await Task.FromResult(Result<PurchaseResponse, AppCoinsPluginSDKError>.CreateError(
                    AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                        errorType: "Error", 
                        message: "Failed to purchase", 
                        description: "AppCoins SDK is not available at AppCoinsSDK.cs:Purchase")
                    ));
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(ResultHandlingJsonCallback))]
    private static void OnPurchaseCompleted(string jsonSuccess, string jsonError)
    {
        if (!string.IsNullOrEmpty(jsonError))
        {
            var sdkError = JsonUtility.FromJson<AppCoinsPluginSDKError>(jsonError);
            if (sdkError != null)
            {
                Instance._tcsPurchase.SetResult(Result<PurchaseResponse, AppCoinsPluginSDKError>.CreateError(sdkError));
            }
            else
            {
                Instance._tcsPurchase.SetResult(Result<PurchaseResponse, AppCoinsPluginSDKError>.CreateError(
                    AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                    errorType: "Error", 
                    message: "Failed to purchase", 
                    description: "Failure in error JSON deserialization at AppCoinsSDK.cs:Purchase")
                ));
            }
        }
        else if (!string.IsNullOrEmpty(jsonSuccess))
        {
            var response = JsonUtility.FromJson<PurchaseResponse>(jsonSuccess);
            if (response != null)
            {
                Instance._tcsPurchase.SetResult(Result<PurchaseResponse, AppCoinsPluginSDKError>.CreateSuccess(response));
            }
            else
            {
                Instance._tcsPurchase.SetResult(Result<PurchaseResponse, AppCoinsPluginSDKError>.CreateError(
                    AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                    errorType: "Error", 
                    message: "Failed to purchase", 
                    description: "Failure in success JSON deserialization at AppCoinsSDK.cs:Purchase")
                ));
            }
        }
        else
        {
            Instance._tcsPurchase.SetResult(Result<PurchaseResponse, AppCoinsPluginSDKError>.CreateError(
                    AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                    errorType: "Error", 
                    message: "Failed to purchase", 
                    description: "Both callback parameters are empty or null at AppCoinsSDK.cs:Purchase")
            ));
        }
    }
    #endif
    #endregion

    #region Get All Purchases

    private TaskCompletionSource<Result<PurchaseData[], AppCoinsPluginSDKError>> _tcsGetAllPurchases;
    public async Task<Result<PurchaseData[], AppCoinsPluginSDKError>> GetAllPurchases()
    {
        #if UNITY_IOS && !UNITY_EDITOR
            this._tcsGetAllPurchases = new TaskCompletionSource<Result<PurchaseData[], AppCoinsPluginSDKError>>();
            _getAllPurchases(OnGetAllPurchasesCompleted);
            return await this._tcsGetAllPurchases.Task;        
        #else
            return await Task.FromResult(Result<PurchaseData[], AppCoinsPluginSDKError>.CreateError(
                AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                    errorType: "Error", 
                    message: "Failed to get all purchases", 
                    description: "AppCoins SDK is not available at AppCoinsSDK.cs:GetAllPurchases")
            ));
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(ResultHandlingJsonCallback))]
        private static void OnGetAllPurchasesCompleted(string jsonSuccess, string jsonError)
        {
            if (!string.IsNullOrEmpty(jsonError))
            {
                var sdkError = JsonUtility.FromJson<AppCoinsPluginSDKError>(jsonError);
                if (sdkError != null)
                {
                    Instance._tcsGetAllPurchases.SetResult(Result<PurchaseData[], AppCoinsPluginSDKError>.CreateError(sdkError));
                }
                else
                {
                    Instance._tcsGetAllPurchases.SetResult(Result<PurchaseData[], AppCoinsPluginSDKError>.CreateError(
                        AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                            errorType: "Error", 
                            message: "Failed to get all purchases", 
                            description: "Failure in error JSON deserialization at AppCoinsSDK.cs:GetAllPurchases")
                    ));
                }
            }
            else if (!string.IsNullOrEmpty(jsonSuccess))
            {
                var response = JsonUtility.FromJson<GetPurchasesResponse>("{\"Purchases\":" + jsonSuccess + "}");
                if (response != null)
                {
                    Instance._tcsGetAllPurchases.SetResult(Result<PurchaseData[], AppCoinsPluginSDKError>.CreateSuccess(response.Purchases));
                }
                else
                {
                    Instance._tcsGetAllPurchases.SetResult(Result<PurchaseData[], AppCoinsPluginSDKError>.CreateError(
                        AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                            errorType: "Error", 
                            message: "Failed to get all purchases", 
                            description: "Failure in success JSON deserialization at AppCoinsSDK.cs:GetAllPurchases")
                    ));
                }
            }
            else
            {
                Instance._tcsPurchase.SetResult(Result<PurchaseResponse, AppCoinsPluginSDKError>.CreateError(
                    AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                        errorType: "Error", 
                        message: "Failed to get all purchases", 
                        description: "Both callback parameters are empty or null at AppCoinsSDK.cs:GetAllPurchases")
                ));
            }
        }
    #endif
    #endregion

    #region Get Latest Purchase 

    private TaskCompletionSource<Result<PurchaseData, AppCoinsPluginSDKError>> _tcsGetLatestPurchase;
    public async Task<Result<PurchaseData, AppCoinsPluginSDKError>> GetLatestPurchase(string sku)
    {
        #if UNITY_IOS && !UNITY_EDITOR
                this._tcsGetLatestPurchase = new TaskCompletionSource<Result<PurchaseData, AppCoinsPluginSDKError>>();
                _getLatestPurchase(sku, OnGetLatestPurchaseCompleted);
                return await this._tcsGetLatestPurchase.Task;        
        #else
                return await Task.FromResult(Result<PurchaseData, AppCoinsPluginSDKError>.CreateError(
                    AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                        errorType: "Error", 
                        message: "Failed to get latest purchase", 
                        description: "AppCoins SDK is not available at AppCoinsSDK.cs:GetLatestPurchase")
                ));
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(ResultHandlingJsonCallback))]
        private static void OnGetLatestPurchaseCompleted(string jsonSuccess, string jsonError)
        {
            if (!string.IsNullOrEmpty(jsonError))
            {
                var sdkError = JsonUtility.FromJson<AppCoinsPluginSDKError>(jsonError);
                if (sdkError != null)
                {
                    Instance._tcsGetLatestPurchase.SetResult(Result<PurchaseData, AppCoinsPluginSDKError>.CreateError(sdkError));
                }
                else
                {
                    Instance._tcsGetLatestPurchase.SetResult(Result<PurchaseData, AppCoinsPluginSDKError>.CreateError(
                        AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                            errorType: "Error", 
                            message: "Failed to get latest purchase", 
                            description: "Failure in error JSON deserialization at AppCoinsSDK.cs:GetLatestPurchase")
                    ));
                }
            }
            else if (!string.IsNullOrEmpty(jsonSuccess))
            {
                var response = JsonUtility.FromJson<PurchaseData>(jsonSuccess);
                if (response != null)
                {
                    Instance._tcsGetLatestPurchase.SetResult(Result<PurchaseData, AppCoinsPluginSDKError>.CreateSuccess(response));
                }
                else
                {
                    Instance._tcsGetLatestPurchase.SetResult(Result<PurchaseData, AppCoinsPluginSDKError>.CreateError(
                        AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                            errorType: "Error", 
                            message: "Failed to get latest purchase", 
                            description: "Failure in success JSON deserialization at AppCoinsSDK.cs:GetLatestPurchase")
                    ));
                }
            }
            else
            {
                Instance._tcsGetLatestPurchase.SetResult(Result<PurchaseData, AppCoinsPluginSDKError>.CreateError(
                        AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                            errorType: "Error", 
                            message: "Failed to get latest purchase", 
                            description: "Both callback parameters are empty or null at AppCoinsSDK.cs:GetLatestPurchase")
                ));
            }
        }
    #endif
    #endregion

    #region Get Unfinished Purchases

    private TaskCompletionSource<Result<PurchaseData[], AppCoinsPluginSDKError>> _tcsGetUnfinishedPurchases;
    public async Task<Result<PurchaseData[], AppCoinsPluginSDKError>> GetUnfinishedPurchases()
    {
        #if UNITY_IOS && !UNITY_EDITOR
                this._tcsGetUnfinishedPurchases = new TaskCompletionSource<Result<PurchaseData[], AppCoinsPluginSDKError>>();
                _getUnfinishedPurchases(OnGetUnfinishedPurchasesCompleted);
                return await this._tcsGetUnfinishedPurchases.Task;        
        #else
                return await Task.FromResult(Result<PurchaseData[], AppCoinsPluginSDKError>.CreateError(
                    AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                        errorType: "Error", 
                        message: "Failed to get unifinished purchases", 
                        description: "AppCoins SDK is not available at AppCoinsSDK.cs:GetUnfinishedPurchases")
                ));
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(ResultHandlingJsonCallback))]
        private static void OnGetUnfinishedPurchasesCompleted(string jsonSuccess, string jsonError)
        {
            if (!string.IsNullOrEmpty(jsonError))
            {
                var sdkError = JsonUtility.FromJson<AppCoinsPluginSDKError>(jsonError);
                if (sdkError != null)
                {
                    Instance._tcsGetUnfinishedPurchases.SetResult(Result<PurchaseData[], AppCoinsPluginSDKError>.CreateError(sdkError));
                }
                else
                {
                    Instance._tcsGetUnfinishedPurchases.SetResult(Result<PurchaseData[], AppCoinsPluginSDKError>.CreateError(
                        AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                            errorType: "Error", 
                            message: "Failed to get unifinished purchases", 
                            description: "Failure in error JSON deserialization at AppCoinsSDK.cs:GetUnfinishedPurchases")
                    ));
                }
            }
            else if (!string.IsNullOrEmpty(jsonSuccess))
            {
                var response = JsonUtility.FromJson<GetPurchasesResponse>("{\"Purchases\":" + jsonSuccess + "}");
                if (response != null)
                {
                    Instance._tcsGetUnfinishedPurchases.SetResult(Result<PurchaseData[], AppCoinsPluginSDKError>.CreateSuccess(response.Purchases));
                }
                else
                {
                    Instance._tcsGetUnfinishedPurchases.SetResult(Result<PurchaseData[], AppCoinsPluginSDKError>.CreateError(
                        AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                            errorType: "Error", 
                            message: "Failed to get unifinished purchases", 
                            description: "Failure in success JSON deserialization at AppCoinsSDK.cs:GetUnfinishedPurchases")
                    ));
                }
            }
            else
            {
                Instance._tcsGetUnfinishedPurchases.SetResult(Result<PurchaseData[], AppCoinsPluginSDKError>.CreateError(
                        AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                            errorType: "Error", 
                            message: "Failed to get unifinished purchases", 
                            description: "Both callback parameters are empty or null at AppCoinsSDK.cs:GetUnfinishedPurchases")
                ));
            }
        }
    #endif
    #endregion

    #region Consume Purchase

    private TaskCompletionSource<Result<ConsumePurchaseResponse, AppCoinsPluginSDKError>> _tcsConsumePurchase;
    public async Task<Result<ConsumePurchaseResponse, AppCoinsPluginSDKError>> ConsumePurchase(string sku)
    {
        #if UNITY_IOS && !UNITY_EDITOR
            this._tcsConsumePurchase = new TaskCompletionSource<Result<ConsumePurchaseResponse, AppCoinsPluginSDKError>>();
            _consumePurchase(sku, OnConsumePurchaseCompleted);
            return await this._tcsConsumePurchase.Task;        
        #else
            return await Task.FromResult(Result<ConsumePurchaseResponse, AppCoinsPluginSDKError>.CreateError(
                AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                    errorType: "Error", 
                    message: "Failed to consume purchase", 
                    description: "AppCoins SDK is not available at AppCoinsSDK.cs:ConsumePurchase")
            ));
        #endif
    }

    #if UNITY_IOS && !UNITY_EDITOR
        [AOT.MonoPInvokeCallback(typeof(ResultHandlingJsonCallback))]
        private static void OnConsumePurchaseCompleted(string jsonSuccess, string jsonError)
        {
            if (!string.IsNullOrEmpty(jsonError))
            {
                var sdkError = JsonUtility.FromJson<AppCoinsPluginSDKError>(jsonError);
                if (sdkError != null)
                {
                    Instance._tcsConsumePurchase.SetResult(Result<ConsumePurchaseResponse, AppCoinsPluginSDKError>.CreateError(sdkError));
                }
                else
                {
                    Instance._tcsConsumePurchase.SetResult(Result<ConsumePurchaseResponse, AppCoinsPluginSDKError>.CreateError(
                        AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                            errorType: "Error", 
                            message: "Failed to consume purchase", 
                            description: "Failure in error JSON deserialization at AppCoinsSDK.cs:ConsumePurchase")
                    ));
                }
            }
            else if (!string.IsNullOrEmpty(jsonSuccess))
            {
                var response = JsonUtility.FromJson<ConsumePurchaseResponse>(jsonSuccess);
                if (response != null)
                {
                    Instance._tcsConsumePurchase.SetResult(Result<ConsumePurchaseResponse, AppCoinsPluginSDKError>.CreateSuccess(response));
                }
                else
                {
                    Instance._tcsConsumePurchase.SetResult(Result<ConsumePurchaseResponse, AppCoinsPluginSDKError>.CreateError(
                        AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                            errorType: "Error", 
                            message: "Failed to consume purchase", 
                            description: "Failure in success JSON deserialization at AppCoinsSDK.cs:ConsumePurchase")
                    ));
                }
            }
            else
            {
                Instance._tcsConsumePurchase.SetResult(Result<ConsumePurchaseResponse, AppCoinsPluginSDKError>.CreateError(
                        AppCoinsPluginSDKError.CreateAppCoinsPluginSDKError(
                            errorType: "Error", 
                            message: "Failed to consume purchase", 
                            description: "Both callback parameters are empty or null at AppCoinsSDK.cs:ConsumePurchase")
                ));
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


