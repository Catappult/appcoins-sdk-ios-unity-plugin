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
    public string sku;
    public string title;
    public string description;
    public string priceCurrency;
    public string priceValue;
    public string priceLabel;
    public string priceSymbol;
}

[Serializable]
public class GetProductsResponse
{
    public ProductData[] Products;
}

[Serializable]
public class PurchaseData
{
    public string uid;
    public string sku;
    public string state;
    public string orderUid;
    public string payload;
    public string created;
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
    public string PurchaseSku;
}

[Serializable]
public class FinishPurchaseResponse
{
    public bool Success;
    public string Error;
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

    [DllImport("__Internal")]
    private static extern void _isAvailable(JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getProducts(string[] skus, int count, JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _purchase(string sku, JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getAllPurchases(JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getLatestPurchase(string sku, JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getUnfinishedPurchases(JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _finishPurchase(string sku, JsonCallback callback);

    public static AppCoinsSDK Instance
    {
        get
        {
            _instance ??= new AppCoinsSDK();
            return _instance;
        }
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

    #region Purchase
    private TaskCompletionSource<PurchaseResponse> _tcsPurchase;
    public async Task<PurchaseResponse> Purchase(string sku)
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsPurchase = new TaskCompletionSource<PurchaseResponse>();
        _purchase(sku, OnPurchaseCompleted);
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
        var response = JsonUtility.FromJson<PurchaseResponse>(json);
        Instance._tcsPurchase.SetResult(response);
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

    #region Finish Purchase
    private TaskCompletionSource<FinishPurchaseResponse> _tcsFinishPurchase;
    public async Task<FinishPurchaseResponse> FinishPurchase(string sku)
    {
#if UNITY_IOS && !UNITY_EDITOR
        this._tcsFinishPurchase = new TaskCompletionSource<FinishPurchaseResponse>();
        _finishPurchase(sku, OnFinishPurchaseCompleted);
        return await this._tcsFinishPurchase.Task;        
#else
        return await Task.FromResult(new FinishPurchaseResponse
        {
            Success = false,
            Error = "AppCoins SDK is not available."
        });
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(JsonCallback))]
    private static void OnFinishPurchaseCompleted(string json)
    {
        var response = JsonUtility.FromJson<FinishPurchaseResponse>(json);
        Instance._tcsFinishPurchase.SetResult(response);
    }
#endif

    #endregion

}


