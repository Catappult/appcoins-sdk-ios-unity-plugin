using System;
using System.Collections.Generic;
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

            var selectedProducts = await AppCoinsSDK.Instance.GetProducts(new string[] { "it.megasoft78.wordsjungle.remove_ads", "it.megasoft78.wordsjungle.small_pack_coins" });

            Debug.Log("Selected products:");

            foreach (var product in selectedProducts)
            {
                Debug.Log($"Product: {product.Sku}, {product.Title}, {product.PriceValue}, {product.PriceCurrency}");
            }

            var products = await AppCoinsSDK.Instance.GetProducts();

            DisplayProducts(products.OrderBy(p => p.PriceValue).ToArray());

            var purchases = await AppCoinsSDK.Instance.GetAllPurchases();

            var unconsumedPurchases = purchases.Where(p => p.State == AppCoinsSDK.PURCHASE_ACKNOWLEDGED).ToArray();

            foreach (var purchase in unconsumedPurchases)
            {
                Debug.Log("Consuming purchase: " + purchase.Sku);
                var response = await AppCoinsSDK.Instance.ConsumePurchase(purchase.Sku);

                if (response.Success)
                {
                    Debug.Log("Purchase consumed successfully");
                }
                else
                {
                    Debug.Log("Error consuming purchase: " + response.Error);
                }
            }

            if (unconsumedPurchases.Length > 0)
            {
                purchases = await AppCoinsSDK.Instance.GetAllPurchases();
            }

            DisplayPurchases(purchases);

            var latestPurchase = await AppCoinsSDK.Instance.GetLatestPurchase("it.megasoft78.wordsjungle.small_pack_coins_almost_free");

            if (latestPurchase != null)
            {
                Debug.Log("Latest purchase: " + latestPurchase.Sku + " - " + this.GetPurchaseStateLabel(latestPurchase.State));
            }

            var unfinishedPurchases = await AppCoinsSDK.Instance.GetUnfinishedPurchases();

            Debug.Log("Unfinished purchases:");

            foreach (var purchase in unfinishedPurchases)
            {
                Debug.Log(purchase.Sku + " - " + this.GetPurchaseStateLabel(purchase.State));
            }
        }
        else
        {
            sdkStatusText.text = "Not Available";

            var products = new List<ProductData>();

            for (int i = 0; i < 8; i++)
            {
                products.Add(new ProductData
                {
                    Sku = $"com.example.coins_{i + 1}00",
                    Title = $"{i + 1}00 Coins",
                    Description = $"{i + 1}00 coins to spend in the game",
                    PriceCurrency = "USD",
                    PriceValue = "0.99",
                    PriceLabel = "$0.99",
                    PriceSymbol = "$"
                });
            };

            DisplayProducts(products.ToArray());

            var purchases = new List<PurchaseData>();

            for (int i = 0; i < 20; i++)
            {
                purchases.Add(new PurchaseData
                {
                    UID = $"purchase_{i + 1}",
                    Sku = $"com.example.coins_{i + 1}00",
                    State = AppCoinsSDK.PURCHASE_ACKNOWLEDGED,
                    OrderUID = $"order_{i + 1}",
                    Payload = $"payload_{i + 1}",
                    Created = System.DateTime.Now.ToString()
                });
            }

            DisplayPurchases(purchases.ToArray());
        }
    }

    public void DisplayProducts(ProductData[] products)
    {
        foreach (var product in products)
        {
            var button = Instantiate(buttonPrefab, panel);
            button.GetComponentInChildren<Text>().text = product.Title;
            button.GetComponent<Button>().onClick.AddListener(() => HandlePurchaseClick(product));
        }
    }

    public void DisplayPurchases(PurchaseData[] purchases)
    {
        purchasesText.text = "Purchases:\n";

        foreach (var purchase in purchases)
        {
            var created = DateTime.Parse(purchase.Created);
            purchasesText.text += created.ToString("yyyy-MM-dd HH:mm:ss") + " => " + purchase.Sku + " - " + this.GetPurchaseStateLabel(purchase.State) + "\n";
        }
    }

    public async void HandlePurchaseClick(ProductData product)
    {
        Debug.Log("HandlePurchaseClick: " + product.Sku);
        var purchaseResponse = await AppCoinsSDK.Instance.Purchase(product.Sku);
        Debug.Log("Purchase state: " + purchaseResponse.State + ", error message: " + purchaseResponse.Error);

        if (purchaseResponse.State == AppCoinsSDK.PURCHASE_STATE_SUCCESS)
        {
            var response = await AppCoinsSDK.Instance.ConsumePurchase(purchaseResponse.PurchaseSku);

            if (response.Success)
            {
                Debug.Log("Purchase consumed successfully");
            }
            else
            {
                Debug.Log("Error consuming purchase: " + response.Error);
            }
        }
    }
}
