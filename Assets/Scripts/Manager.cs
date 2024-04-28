using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class Manager : MonoBehaviour
{
    public Text sdkStatusText;
    public Transform panel;
    public GameObject buttonPrefab;
    public Text purchasesText;

    public string GetPurchaseStateLabel(string state)
    {
        switch (state)
        {
            case AppCoinsSDK.PURCHASE_PENDING:
                return "Pending";
            case AppCoinsSDK.PURCHASE_ACKNOWLEDGED:
                return "Acknowledged";
            case AppCoinsSDK.PURCHASE_CONSUMED:
                return "Consumed";
            default:
                return "Unknown";
        }
    }

    private async void Awake()
    {
        sdkStatusText.text = "Loading...";

        var sdkAvailable = await AppCoinsSDK.Instance.IsAvailable();

        if (sdkAvailable)
        {
            sdkStatusText.text = "Available";
        }
        else
        {
            sdkStatusText.text = "Not Available";
        }

        var selectedProducts = await AppCoinsSDK.Instance.GetProducts(new string[] { "it.megasoft78.wordsjungle.remove_ads", "it.megasoft78.wordsjungle.small_pack_coins" });

        Debug.Log("Selected products:");

        foreach (var product in selectedProducts)
        {
            Debug.Log($"Product: {product.sku}, {product.title}, {product.priceValue}, {product.priceCurrency}");
        }

        var products = await AppCoinsSDK.Instance.GetProducts();

        foreach (var product in products.OrderBy(p => p.priceValue))
        {
            var button = Instantiate(buttonPrefab, panel);
            button.GetComponentInChildren<Text>().text = product.title;
            button.GetComponent<Button>().onClick.AddListener(() => HandlePurchaseClick(product));
        }

        var purchases = await AppCoinsSDK.Instance.GetAllPurchases();

         purchasesText.text = "Purchases:\n";

        foreach (var purchase in purchases)
        {
            purchasesText.text += purchase.sku + " - " + this.GetPurchaseStateLabel(purchase.state) + "\n";
        }

        var latestPurchase = await AppCoinsSDK.Instance.GetLatestPurchase("it.megasoft78.wordsjungle.small_pack_coins_almost_free");

        if (latestPurchase != null)
        {
            Debug.Log("Latest purchase: " + latestPurchase.sku + " - " + this.GetPurchaseStateLabel(latestPurchase.state));
        }

        var unfinishedPurchases = await AppCoinsSDK.Instance.GetUnfinishedPurchases();

        Debug.Log("Unfinished purchases:");

        foreach (var purchase in unfinishedPurchases)
        {
            Debug.Log(purchase.sku + " - " + this.GetPurchaseStateLabel(purchase.state));
        }
    }

    public async void HandlePurchaseClick(ProductData product)
    {
        Debug.Log("HandlePurchaseClick: " + product.sku);
        var purchaseResponse = await AppCoinsSDK.Instance.Purchase(product.sku);
        Debug.Log("Purchase state: " + purchaseResponse.state + ", error message: " + purchaseResponse.error);
    }
}
