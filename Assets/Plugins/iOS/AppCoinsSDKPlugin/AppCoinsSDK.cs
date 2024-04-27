using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Linq;

[Serializable]
public class ProductsWrapper
{
    public ProductData[] items;
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
public class PurchaseResponse
{
    public string state;
    public string error;
}

[Serializable]
public class PurchasesWrapper
{
    public PurchaseData[] purchases;
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
public class ListPurchasesResponse
{
    public PurchaseData[] purchases;
}

public class ProductsReceivedEventArgs : EventArgs
{
    public bool Available { get; set; }
    public ProductData[] Products { get; set; }
}

public class ListPurchasesReceivedEventArgs : EventArgs
{
    public bool Available { get; set; }
    public PurchaseData[] Purchases { get; set; }
}

public class AppCoinsSDK
{
    public const string PURCHASE_PENDING = "PENDING";
    public const string PURCHASE_ACKNOWLEDGED = "ACKNOWLEDGED";
    public const string PURCHASE_CONSUMED = "CONSUMED";


    private static AppCoinsSDK _instance;
    private delegate void InitializeCallback(string result);

    public delegate void ProductsReceivedEventHandler(object sender, ProductsReceivedEventArgs e);
    public event ProductsReceivedEventHandler OnProductsReceived;
    public delegate void ListPurchasesReceivedEventHandler(object sender, ListPurchasesReceivedEventArgs e);
    public event ListPurchasesReceivedEventHandler OnListPurchasesReceived;


    [DllImport("__Internal")]
    private static extern void _initialize(InitializeCallback callback);

    [DllImport("__Internal")]
    private static extern void _purchase(string sku, Action<string> callback);

    [DllImport("__Internal")]
    private static extern void _listPurchases(Action<string> callback);

    public static AppCoinsSDK Instance
    {
        get
        {
            _instance ??= new AppCoinsSDK();
            return _instance;
        }
    }

    public void Initialize()
    {
#if UNITY_IOS && !UNITY_EDITOR
        _initialize(OnInitializeCompleteStatic);
#else
        Debug.Log("Not on iOS. Using fake products");

        SimulateProductsReception();
#endif
    }

    private void SimulateProductsReception()
    {
        ProductData[] products = new ProductData[]
        {
            new() {
                sku = "gem_pack_100",
                title = "100 Gems",
                description = "A pack of 100 gems to use in-game for buying power-ups or other items.",
                priceCurrency = "USD",
                priceValue = "1.99",
                priceLabel = "1.99 USD",
                priceSymbol = "$"
            },
            new() {
                sku = "premium_skin",
                title = "Premium Warrior Skin",
                description = "An exclusive skin for your warrior character, available permanently.",
                priceCurrency = "USD",
                priceValue = "4.99",
                priceLabel = "4.99 USD",
                priceSymbol = "$"
            },
            new() {
                sku = "energy_refill",
                title = "Energy Refill",
                description = "Instantly refills your energy to maximum, allowing you to play longer without waiting.",
                priceCurrency = "USD",
                priceValue = "0.99",
                priceLabel = "0.99 USD",
                priceSymbol = "$"
            },
            new() {
                sku = "ultimate_bundle",
                title = "Ultimate Bundle",
                description = "Includes 500 gems, two premium skins, and three energy refills.",
                priceCurrency = "USD",
                priceValue = "19.99",
                priceLabel = "19.99 USD",
                priceSymbol = "$"
            }
        };

        HandleOnProductsReceived(products, false);
    }

#if UNITY_IOS && !UNITY_EDITOR
    [AOT.MonoPInvokeCallback(typeof(InitializeCallback))]
    private static void OnInitializeCompleteStatic(string jsonResult)
    {
        _instance?.OnInitializeComplete(jsonResult);
    }
#endif

    public void OnInitializeComplete(string jsonResult)
    {
        var productsWrapper = JsonUtility.FromJson<ProductsWrapper>("{\"items\":" + jsonResult + "}");
        HandleOnProductsReceived(productsWrapper.items, true);
    }

    private void HandleOnProductsReceived(ProductData[] products, bool available)
    {
        OnProductsReceived?.Invoke(this, new ProductsReceivedEventArgs
        {
            Available = available,
            Products = products.OrderBy(p => p.priceValue).ToArray()
        });
    }

    public void Purchase(string sku)
    {
#if UNITY_IOS && !UNITY_EDITOR
    _purchase(sku, OnPurchaseComplete);
#else
        Debug.Log("Purchasing not supported on this platform.");
        OnPurchaseComplete("{ \"state\": \"verified\"}");
#endif
    }

    [MonoPInvokeCallback(typeof(Action<string>))]
    private static void OnPurchaseComplete(string jsonResult)
    {
        Debug.Log($"Received purchase result: {jsonResult}");
        // Parse JSON result
        var purchaseResponse = JsonUtility.FromJson<PurchaseResponse>(jsonResult);
        switch (purchaseResponse.state)
        {
            case "verified":
                Debug.Log("Purchase verified!");
                // Handle verified purchase (e.g., grant item)
                break;
            case "pending":
                Debug.Log("Purchase is pending.");
                // Handle pending state
                break;
            case "error":
                Debug.LogError($"Error during purchase: {purchaseResponse.error}");
                // Handle error
                break;
        }
    }

    public void ListPurchases()
    {
#if UNITY_IOS && !UNITY_EDITOR
    _listPurchases(OnListPurchaseCompleteStatic);
#else
        Debug.Log("List purchases not supported on this platform.");
        SimulatePurchasesReception();
#endif
    }

    private void SimulatePurchasesReception()
    {
        var purchasesResponse = new PurchasesWrapper
        {
            purchases = new PurchaseData[] {
                new() {
                    uid = Guid.NewGuid().ToString(),
                    sku = "gem_pack_100",
                    state = PURCHASE_ACKNOWLEDGED,
                    orderUid = Guid.NewGuid().ToString(),
                    payload = "{ fakeData: true}",
                    created = "2021-01-01T00:00:00Z"
                },
                new() {
                    uid = Guid.NewGuid().ToString(),
                    sku = "premium_skin",
                    state = PURCHASE_CONSUMED,
                    orderUid = Guid.NewGuid().ToString(),
                    payload = "{ fakeData: true}",
                    created = "2021-01-02T00:00:00Z"
                },
                new() {
                    uid = Guid.NewGuid().ToString(),
                    sku = "energy_refill",
                    state = PURCHASE_ACKNOWLEDGED,
                    orderUid = Guid.NewGuid().ToString(),
                    payload = "{ fakeData: true}",
                    created = "2021-01-03T00:00:00Z"
                },
            }
        };

        OnListPurchaseComplete(JsonUtility.ToJson(purchasesResponse));
    }

#if UNITY_IOS && !UNITY_EDITOR
    [MonoPInvokeCallback(typeof(Action<string>))]
    private static void OnListPurchaseCompleteStatic(string jsonResult)
    {
        _instance?.OnListPurchaseComplete(jsonResult);
    }
#endif

    private void OnListPurchaseComplete(string jsonResult)
    {
        Debug.Log($"Received list purchases result: {jsonResult}");

        var purchasesWrapper = JsonUtility.FromJson<PurchasesWrapper>("{\"purchases\":" + jsonResult + "}");

        _instance?.OnListPurchasesReceived?.Invoke(_instance, new ListPurchasesReceivedEventArgs
        {
            Purchases = purchasesWrapper.purchases
        });
    }
}
