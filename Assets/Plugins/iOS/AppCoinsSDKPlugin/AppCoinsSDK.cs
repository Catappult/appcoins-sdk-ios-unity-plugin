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
public class PurchaseResponse
{
    public string state;
    public string error;
}

public class AppCoinsSDK
{
    private static AppCoinsSDK _instance;
    private delegate void JsonCallback(string result);

    [DllImport("__Internal")]
    private static extern void _isAvailable(JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _getProducts(string[] skus, int count, JsonCallback callback);

    [DllImport("__Internal")]
    private static extern void _purchase(string sku, JsonCallback callback);

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
            state = "error",
            error = "AppCoins SDK is not available."
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

}
