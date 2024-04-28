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
            Debug.Log($"Product: {product.Sku}, {product.Title}, {product.PriceValue}, {product.PriceCurrency}");
        }

        var products = await AppCoinsSDK.Instance.GetProducts();

        foreach (var product in products.OrderBy(p => p.PriceValue))
        {
            var button = Instantiate(buttonPrefab, panel);
            button.GetComponentInChildren<Text>().text = product.Title;
            button.GetComponent<Button>().onClick.AddListener(() => HandlePurchaseClick(product));
        }

        var purchases = await AppCoinsSDK.Instance.GetAllPurchases();

         purchasesText.text = "Purchases:\n";

        foreach (var purchase in purchases)
        {
            purchasesText.text += purchase.Created + ": " +  purchase.Sku + " - " + this.GetPurchaseStateLabel(purchase.State) + "\n";
        }

        var latestPurchase = await AppCoinsSDK.Instance.GetLatestPurchase("it.megasoft78.wordsjungle.small_pack_coins_almost_free");

        if (latestPurchase != null)
        {
            Debug.Log("Latest purchase: " + latestPurchase.Sku + " - " + this.GetPurchaseStateLabel(latestPurchase.State));
        }

        var unfinishedPurchases = await AppCoinsSDK.Instance.GetUnfinishedPurchases();

        Debug.Log("Unfinished purchases:");

        foreach (var purchase in unfinishedPurchases)
        {
            Debug.Log("Finishing purchase: " + purchase.Sku);
            var response = await AppCoinsSDK.Instance.FinishPurchase(purchase.Sku);

            if (response.Success)
            {
                Debug.Log("Purchase finished successfully");
            }
            else
            {
                Debug.Log("Error finishing purchase: " + response.Error);
            }
        }
    }

    public async void HandlePurchaseClick(ProductData product)
    {
        Debug.Log("HandlePurchaseClick: " + product.Sku);
        var purchaseResponse = await AppCoinsSDK.Instance.Purchase(product.Sku);
        Debug.Log("Purchase state: " + purchaseResponse.State + ", error message: " + purchaseResponse.Error);

        if (purchaseResponse.State == AppCoinsSDK.PURCHASE_STATE_SUCCESS)
        {
            var response = await AppCoinsSDK.Instance.FinishPurchase(purchaseResponse.PurchaseSku);

            if (response.Success)
            {
                Debug.Log("Purchase finished successfully");
            }
            else
            {
                Debug.Log("Error finishing purchase: " + response.Error);
            }
        }
    }
}
