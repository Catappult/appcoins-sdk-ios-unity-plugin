using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class Manager : MonoBehaviour
{
    public Text sdkStatusText;
    public Transform panel;
    public GameObject buttonPrefab;
    public Text purchasesText;

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

        var products = await AppCoinsSDK.Instance.GetProducts();

        foreach (var product in products.OrderBy(p => p.priceValue))
        {
            var button = Instantiate(buttonPrefab, panel);
            button.GetComponentInChildren<Text>().text = product.title;
            button.GetComponent<Button>().onClick.AddListener(() => HandlePurchaseClick(product));
        }
    }

    public async void HandlePurchaseClick(ProductData product)
    {
        Debug.Log("HandlePurchaseClick: " + product.sku);
        var purchaseResponse = await AppCoinsSDK.Instance.Purchase(product.sku);
        Debug.Log("Purchase state: " + purchaseResponse.state + ", error message: " + purchaseResponse.error);
    }
}
