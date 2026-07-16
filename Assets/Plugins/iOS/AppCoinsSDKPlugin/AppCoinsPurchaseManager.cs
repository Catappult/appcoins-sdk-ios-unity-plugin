using System;
using UnityEngine;
using AppCoins.Internal;

// Internal runtime receiver for AppCoins purchase-intent updates.
//
// The native layer (UnityPlugin.swift) delivers indirect / deep-link purchase
// intents by calling UnitySendMessage("AppCoinsPurchaseManager",
// "OnPurchaseUpdatedInternal", json). Unity resolves that by GameObject NAME,
// so this component must live on a GameObject named exactly
// "AppCoinsPurchaseManager" and expose a public OnPurchaseUpdatedInternal(string)
// method. Both are kept for that native contract.
//
// This is no longer a public API surface: intents are routed to the Unity IAP
// store adapter (see AppCoins.Unity.AppCoinsIAP) and surfaced to developers as
// the standard Unity IAP OnPurchasePending event.
internal class AppCoinsPurchaseManager : MonoBehaviour
{
    private static AppCoinsPurchaseManager _instance;

    // Consumed internally by the Unity IAP store adapter.
    internal static event Action<PurchaseIntent> OnPurchaseIntent;

    internal static AppCoinsPurchaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var obj = new GameObject("AppCoinsPurchaseManager");
                _instance = obj.AddComponent<AppCoinsPurchaseManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        AppCoinsNativeBridge.StartObservingPurchases();
    }

    // Invoked by the native layer via UnitySendMessage. Name must not change.
    public void OnPurchaseUpdatedInternal(string purchaseIntentJson)
    {
        var intent = JsonUtility.FromJson<PurchaseIntent>(purchaseIntentJson);
        OnPurchaseIntent?.Invoke(intent);
    }
}
