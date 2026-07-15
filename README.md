# AppCoins Unity Plugin — iOS

The iOS Billing SDK registers AppCoins as a **Unity IAP v5 custom store**. If you already have Unity IAP 5 set up, adding AppCoins takes **one line of code**.

When the app runs under Aptoide alternative distribution, purchases are routed through AppCoins. When it runs through the Apple App Store, Unity IAP uses StoreKit as usual. Your purchasing code stays identical in both cases.

---

## Quick Start

**1.** Download the latest `.unitypackage` from the [Releases page](https://github.com/Catappult/appcoins-sdk-ios-unity-plugin/releases) and import it via **Assets > Import Package > Custom Package**.

**2.** Add one line before your existing `Connect()` call:

```csharp
using AppCoins.Unity;

// Add this before StoreController.Connect()
await AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic);
```

That's it. AppCoins is now active alongside your existing Unity IAP integration.

---

## How it works

`ConfigureStoreAsync(Automatic)` calls the AppCoins `isAvailable` check at runtime:

- **Aptoide alternative distribution** → purchases go through AppCoins.
- **Apple App Store** → Unity IAP uses StoreKit as normal.

You can also force a specific store if needed:

```csharp
AppCoinsStoreMode.Aptoide  // always use AppCoins
AppCoinsStoreMode.Apple    // always use Apple
```

---

## Don't have a Unity IAP 5 integration yet?

If you are starting from scratch, the full implementation from connect to confirm looks like this:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using AppCoins.Unity;

public class Shop : MonoBehaviour
{
    private StoreController _controller;
    private Product _gasProduct;

    private async void Start()
    {
        await AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic);

        _controller = UnityIAPServices.StoreController();
        _controller.OnProductsFetched  += OnProductsFetched;
        _controller.OnPurchasesFetched += OnPurchasesFetched;
        _controller.OnPurchasePending  += OnPurchasePending;
        _controller.OnPurchaseFailed   += order => Debug.LogWarning("Purchase failed: " + order.FailureReason);

        try { await _controller.Connect(); }
        catch (Exception e) { Debug.LogError("Store connection failed: " + e.Message); return; }

        _controller.FetchProducts(new List<ProductDefinition>
        {
            new ProductDefinition("gas", ProductType.Consumable),
        });
    }

    private void OnProductsFetched(List<Product> products)
    {
        _gasProduct = products.FirstOrDefault(p => p.definition.id == "gas");
        _controller.FetchPurchases(); // recover unfinished purchases on launch
    }

    private void OnPurchasesFetched(Orders orders)
    {
        foreach (var pending in orders.PendingOrders)
            ProcessPendingOrder(pending);
    }

    public void BuyGas()
    {
        if (_gasProduct != null) _controller.PurchaseProduct(_gasProduct);
    }

    private void OnPurchasePending(PendingOrder order)
    {
        ProcessPendingOrder(order);
    }

    private void ProcessPendingOrder(PendingOrder order)
    {
        string productId = order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id;
        if (productId == "gas") AddGas();
        _controller.ConfirmPurchase(order);
    }

    private void AddGas() { /* your game logic */ }
}
```

> ⚠️ **Always call `FetchPurchases()` on startup.** If the app closes after payment but before `ConfirmPurchase` runs, the purchase stays unfinished. The AppCoins SDK refunds unfinished purchases after 24 hours. Recovering them on every launch guarantees users always receive what they paid for.

A fully runnable version is at `Assets/Plugins/iOS/AppCoinsSDKPlugin/Samples/TrivialDriveIAP.cs`.

### Requirements

- Unity 2022.3 or later
- Unity In-App Purchasing (`com.unity.purchasing`) 5.4.0 or later
- iOS 17.4 or higher

### Installing Unity IAP 5

Open **Window > Package Manager**, click **+**, select **Add package by name**, and enter `com.unity.purchasing` version `5.4.0`.

---

## Server-Side Receipt Verification

On `OnPurchasePending`, `order.Info.Receipt` contains a JSON receipt with the shape `{ "Store", "TransactionID", "Payload" }`. The `Payload` carries the AppCoins purchase and its signature, which your backend can use to verify the transaction via the Catappult validation endpoint.

Confirming purchases client-side is fine during development, but **always add server-side validation before shipping to production**.

---

## Testing

### Enabling the Aptoide store in Xcode

To test the AppCoins store on a development build, simulate alternative distribution in Xcode:

1. In your target's **Build Settings**, search for **Marketplaces**.
2. Under **Deployment**, set **Marketplaces** to `com.aptoide.ios.store`.
3. Open your **scheme → Run → Options** and set **Distribution** to `com.aptoide.ios.store`.

See [Apple's documentation](https://developer.apple.com/documentation/appdistribution/distributing-your-app-on-an-alternative-marketplace#Test-your-app-during-development) for more detail.

### Toggling between Aptoide and Apple in one build

When using `AppCoinsStoreMode.Automatic`, you can flip which store is active at runtime via a deep link. Open the device browser and navigate to:

```
{bundleId}.iap://wallet.appcoins.io/default?value={true|false}
```

- `value=true` → enables the AppCoins store.
- `value=false` → disables it; `Automatic` falls back to Apple.

### Sandbox

To simulate purchases without real payments, use the AppCoins sandbox environment: [Sandbox documentation](https://docs.connect.aptoide.com/docs/ios-sandbox-environment).

---

## Not on Unity IAP 5 yet?

Unity IAP v5 is our recommended integration path. That said, migrating an existing IAP implementation is not always practical in the short term.

If you are on Unity IAP 4.x (or not using Unity IAP at all), the previous version of the AppCoins plugin is still available and fully supported. Use the [legacy plugin release](https://github.com/Catappult/appcoins-sdk-ios-unity-plugin/releases) and its corresponding documentation until you are ready to move to v5.

> Note: Unity IAP 4.x and 5.x cannot coexist in the same project.

---

## Migrating from the Legacy AppCoins API

If you were previously using the bespoke `AppCoinsSDK.Instance` async API, the table below maps every call to its Unity IAP v5 equivalent:

| Legacy API | Unity IAP v5 equivalent |
|---|---|
| `AppCoinsSDK.Instance.IsAvailable()` | `AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic)` |
| `AppCoinsSDK.Instance.GetProducts(skus)` | `StoreController.FetchProducts(defs)` → `OnProductsFetched` |
| `AppCoinsSDK.Instance.Purchase(sku)` | `StoreController.PurchaseProduct(product)` → `OnPurchasePending` |
| `AppCoinsSDK.Instance.ConsumePurchase(sku)` | `StoreController.ConfirmPurchase(order)` → `OnPurchaseConfirmed` |
| `AppCoinsSDK.Instance.GetAllPurchases()` / `GetUnfinishedPurchases()` | `StoreController.FetchPurchases()` → `OnPurchasesFetched` |
| `AppCoinsSDK.Instance.GetLatestPurchase(sku)` | `StoreController.CheckEntitlement(product)` → `OnCheckEntitlement` |
| `AppCoinsPurchaseManager.OnPurchaseUpdated` | `StoreController.OnPurchasePending` (surfaced automatically) |
