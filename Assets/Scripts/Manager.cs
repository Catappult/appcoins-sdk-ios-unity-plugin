using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Manager : MonoBehaviour
{
    public Text sdkStatusText;
    public Transform panel;
    public GameObject buttonPrefab;
    public Text purchasesText;

    private void Awake()
    {
        StartCoroutine(DelayedInitialize());
        sdkStatusText.text = "Loading...";
    }

    private IEnumerator DelayedInitialize()
    {
        yield return new WaitForSeconds(1f);
        AppCoinsSDK.Instance.OnProductsReceived += HandleProductsReceived;
        AppCoinsSDK.Instance.OnListPurchasesReceived += HandleListPurchasesReceived;
        AppCoinsSDK.Instance.Initialize();
    }

    private void HandleProductsReceived(object sender, ProductsReceivedEventArgs e)
    {
        sdkStatusText.text = e.Available ? "Available" : "Not Available";

        foreach (var product in e.Products)
        {
            Debug.Log(JsonUtility.ToJson(product));
            var button = Instantiate(buttonPrefab, panel);
            button.GetComponentInChildren<Text>().text = product.title;
            button.GetComponent<Button>().onClick.AddListener(() => HandlePurchaseClick(product));
        }

        StartCoroutine(DelayedListPurchases());
    }

    public void HandlePurchaseClick(ProductData product)
    {
        Debug.Log("HandlePurchaseClick: " + product.sku);
        AppCoinsSDK.Instance.Purchase(product.sku);
    }

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

    public IEnumerator DelayedListPurchases()
    {
        yield return new WaitForSeconds(1f);
        AppCoinsSDK.Instance.ListPurchases();
    }

    public void HandleListPurchasesReceived(object sender, ListPurchasesReceivedEventArgs e)
    {
        purchasesText.text = "Purchases:\n";

        foreach (var purchase in e.Purchases)
        {
            purchasesText.text += purchase.sku + " - " + this.GetPurchaseStateLabel(purchase.state) + "\n";
        }
    }
}
