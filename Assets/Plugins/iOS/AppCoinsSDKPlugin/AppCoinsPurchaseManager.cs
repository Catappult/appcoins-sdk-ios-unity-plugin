using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// This class must match the name "AppCoinsPurchaseManager"
public class AppCoinsPurchaseManager : MonoBehaviour
{
    private static AppCoinsPurchaseManager _instance;

    // Subscribe to get notified of non-direct IAP
    public static event Action<PurchaseResponse> OnPurchaseUpdated;

    public static AppCoinsPurchaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("AppCoinsPurchaseManager");
                _instance = obj.AddComponent<AppCoinsPurchaseManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        AppCoinsSDK.StartObservingPurchases();
    }

    // This method name must match "OnPurchaseUpdatedInternal" from Swift
    public void OnPurchaseUpdatedInternal(string purchaseJson)
    {
        PurchaseResponse purchaseResponse = JsonUtility.FromJson<PurchaseResponse>(purchaseJson);
        NotifyPurchase(purchaseResponse);
    }

    public static void NotifyPurchase(PurchaseResponse purchaseResponse)
    {
        OnPurchaseUpdated?.Invoke(purchaseResponse);
    }
}