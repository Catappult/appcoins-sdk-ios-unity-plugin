# AppCoins Unity Plugin — iOS

The iOS Billing SDK is a simple solution to implement Aptoide billing. Its Unity Plugin registers AppCoins as a **Unity IAP v5 custom store**, so your game uses the standard Unity In-App Purchasing API (`StoreController`) and AppCoins backs the purchases on iOS alternative distribution. When the same game ships through the Apple App Store, Unity IAP transparently uses Apple's StoreKit instead — your purchasing code stays identical.

The SDK automatically handles transaction reporting to Apple for Core Technology Commission (CTC) calculation, removing this burden from developers.

---

## Requirements

- Unity 2022.3 or later
- Unity In-App Purchasing (`com.unity.purchasing`) 5.4.0 or later
- iOS 17.4 or higher

---

## Installation

**1. Add Unity In-App Purchasing.**
Open **Window > Package Manager**, click **+**, select **Add package by name**, and enter:

```
com.unity.purchasing   5.4.0
```

**2. Add the AppCoins Plugin.**
Download the latest `.unitypackage` from the [Releases page](https://github.com/Catappult/appcoins-sdk-ios-unity-plugin/releases), then import it via **Assets > Import Package > Custom Package**. The plugin registers itself automatically on iOS — no extra setup required.

---

## Integration

Add `using AppCoins.Unity;` to your purchasing script alongside `using UnityEngine.Purchasing;`. Then follow these steps:

### 1 — Configure the store

Call this **once**, before connecting to the store. It tells Unity IAP which store backs your purchases:

```csharp
await AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic);
```

`Automatic` (recommended) uses the AppCoins `isAvailable` check: it picks Aptoide when the app is running under alternative distribution, and the Apple App Store otherwise. You can also pass `AppCoinsStoreMode.Aptoide` or `AppCoinsStoreMode.Apple` to force one explicitly.

On non-iOS platforms this call is a no-op.

### 2 — Connect, fetch products, and handle purchases

From here it is standard Unity IAP v5:

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

        // Always call FetchPurchases on startup to recover any purchase the
        // user paid for but that was not consumed (e.g. the app crashed).
        _controller.FetchPurchases();
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

        // Grant the item to the player.
        if (productId == "gas") AddGas();

        // Consume the order so the consumable can be purchased again.
        _controller.ConfirmPurchase(order);
    }

    private void AddGas() { /* your game logic */ }
}
```

> ⚠️ **Always call `FetchPurchases()` on startup.** If the app is closed after payment but before `ConfirmPurchase` runs, the purchase is unfinished. The AppCoins SDK refunds unfinished purchases after 24 hours. Recovering them on every launch guarantees users always receive what they paid for.

A fully runnable sample is available at `Assets/Plugins/iOS/AppCoinsSDKPlugin/Samples/TrivialDriveIAP.cs`.

---

## Server-Side Receipt Verification

On `OnPurchasePending`, `order.Info.Receipt` contains a JSON receipt with the shape `{ "Store", "TransactionID", "Payload" }`. The `Payload` carries the AppCoins purchase and its signature, which your backend can forward to the Catappult validation endpoint or verify directly.

If you do not yet have a backend, you can confirm purchases client-side during development — but **always add server-side validation before shipping to production**.

---

## Testing

### Enabling the Aptoide store in Xcode

To test the AppCoins store on a development build, simulate alternative distribution in Xcode:

1. In your target's **Build Settings**, search for **Marketplaces**.
2. Under **Deployment**, set **Marketplaces** to `com.aptoide.ios.store`.
3. Open your **scheme → Run → Options** and set **Distribution** to `com.aptoide.ios.store`.

See [Apple's documentation](https://developer.apple.com/documentation/appdistribution/distributing-your-app-on-an-alternative-marketplace#Test-your-app-during-development) for more detail.

### Toggling between Aptoide and Apple in one build

When using `AppCoinsStoreMode.Automatic`, you can flip which store is active at runtime via a deep link. Open the device's browser and navigate to:

```
{bundleId}.iap://wallet.appcoins.io/default?value={true|false}
```

- `value=true` → enables the AppCoins store.
- `value=false` → disables it; `Automatic` falls back to Apple.

### Sandbox

To simulate purchases without real payments, use the AppCoins sandbox environment: [Sandbox documentation](https://docs.connect.aptoide.com/docs/ios-sandbox-environment).

---

## Not on Unity IAP 5 yet?

Unity IAP v5 is the recommended way to integrate this plugin, and moving to it from v4 is the best long-term path — the API is cleaner and the integration with AppCoins is transparent.

That said, **migrating a full IAP implementation is not always practical in the short term.** If you are on Unity IAP 4.x (or not using Unity IAP at all), the previous version of the AppCoins plugin is fully supported and still available. Use the [legacy plugin release](https://github.com/Catappult/appcoins-sdk-ios-unity-plugin/releases) and its corresponding documentation until you are ready to move to v5.

> You cannot run Unity IAP 4.x and 5.x in the same project — pick one version and stay on it.

---

## Migrating from the Legacy AppCoins API

If you are already on Unity IAP v5 but were previously using the bespoke `AppCoinsSDK.Instance` async API, the table below maps every call to its v5 equivalent:

| Legacy API | Unity IAP v5 equivalent |
|---|---|
| `AppCoinsSDK.Instance.IsAvailable()` | `AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic)` |
| `AppCoinsSDK.Instance.GetProducts(skus)` | `StoreController.FetchProducts(defs)` → `OnProductsFetched` |
| `AppCoinsSDK.Instance.Purchase(sku)` | `StoreController.PurchaseProduct(product)` → `OnPurchasePending` |
| `AppCoinsSDK.Instance.ConsumePurchase(sku)` | `StoreController.ConfirmPurchase(order)` → `OnPurchaseConfirmed` |
| `AppCoinsSDK.Instance.GetAllPurchases()` / `GetUnfinishedPurchases()` | `StoreController.FetchPurchases()` → `OnPurchasesFetched` |
| `AppCoinsSDK.Instance.GetLatestPurchase(sku)` | `StoreController.CheckEntitlement(product)` → `OnCheckEntitlement` |
| `AppCoinsPurchaseManager.OnPurchaseUpdated` | `StoreController.OnPurchasePending` (surfaced automatically) |
