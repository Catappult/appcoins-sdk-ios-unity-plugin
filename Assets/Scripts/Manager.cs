using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class Manager : MonoBehaviour
{
    public Text sdkStatusText;
    public Transform panel;
    public GameObject buttonPrefab;

    private void Awake()
    {
        StartCoroutine(DelayedInitialize());
        sdkStatusText.text = "Loading...";
    }

    private IEnumerator DelayedInitialize()
    {
        yield return new WaitForSeconds(1f);
        AppCoinsSDK.Instance.OnProductsReceived += HandleProductsReceived;
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
    }

    public void HandlePurchaseClick(ProductData product)
    {
        Debug.Log("HandlePurchaseClick: " + product.sku);
        AppCoinsSDK.Instance.Purchase(product.sku);
    }
}
