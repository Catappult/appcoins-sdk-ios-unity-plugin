using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// This class must match the name "AppCoinsPurchaseManager"
public class AppCoinsPurchaseManager : MonoBehaviour
{
    private static AppCoinsPurchaseManager _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitOnLoad()
    {
        _ = Instance; // Ensures the singleton is created at app startup
    }

    // Subscribe to get notified of indirect IAP
    public static event Action<PurchaseIntent> OnPurchaseUpdated;

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
    public void OnPurchaseUpdatedInternal(string purchaseIntentJson)
    {
        PurchaseIntent purchaseIntent = JsonUtility.FromJson<PurchaseIntent>(purchaseIntentJson);
        NotifyPurchase(purchaseIntent);
    }

    public static void NotifyPurchase(PurchaseIntent purchaseIntent)
    {
        OnPurchaseUpdated?.Invoke(purchaseIntent);
    }
}