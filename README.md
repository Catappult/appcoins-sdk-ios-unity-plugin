# AppCoins Unity Plugin — iOS

**Unity In-App Purchasing (Unity IAP)** is the industry-standard way to sell items in Unity games. It provides a single, unified API that works across every platform — iOS, Android, and others — so you write your purchasing code once and it runs everywhere. Under the hood, Unity IAP talks to each platform's native billing system (StoreKit on iOS, Google Play Billing on Android, and so on) so you don't have to.

The AppCoins Unity Plugin plugs into this system as a **custom store**. When your game runs on Aptoide alternative distribution, purchases are routed through AppCoins automatically. When it runs through the Apple App Store, Unity IAP uses StoreKit as usual. **Your code never changes between the two.**

If you already have Unity IAP 5 running in your project, adding AppCoins takes **one line of code**.

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

`Automatic` (recommended) uses the AppCoins `isAvailable` check at runtime: it picks Aptoide when the app is running under alternative distribution, and Apple's StoreKit otherwise. You can also pass `AppCoinsStoreMode.Aptoide` or `AppCoinsStoreMode.Apple` to force one explicitly.

---

## Don't have a Unity IAP 5 integration yet?

Unity IAP 5 is easy to set up from scratch — the official [Unity IAP documentation](https://docs.unity3d.com/Packages/com.unity.purchasing@5.4/manual/index.html) is a good reference, and the integration below covers everything you need to get purchases working with AppCoins.

### Requirements

- Unity 2022.3 or later
- Unity In-App Purchasing (`com.unity.purchasing`) 5.4.0 or later
- iOS 17.4 or higher

### Installing Unity IAP 5

1. Open **Window > Package Manager**.
2. Click **+** and select **Add package by name**.
3. Enter `com.unity.purchasing` as the name and `5.4.0` as the version, then click **Add**.

Unity resolves the package and its dependencies automatically. You can verify the installation by checking that `com.unity.purchasing` appears in your `Packages/manifest.json`.

### Full implementation

The Unity IAP v5 store flow has four stages: connect → fetch products → purchase → confirm. Here is a complete, working example:

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
        // Select the store before connecting.
        await AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic);

        // Get a StoreController and subscribe to its events.
        // The StoreController is the central Unity IAP object — it manages
        // the connection to the store, the product catalogue, and all purchase
        // callbacks. See: https://docs.unity3d.com/Packages/com.unity.purchasing@5.4/manual/StoreController.html
        _controller = UnityIAPServices.StoreController();
        _controller.OnProductsFetched  += OnProductsFetched;
        _controller.OnPurchasesFetched += OnPurchasesFetched;
        _controller.OnPurchasePending  += OnPurchasePending;
        _controller.OnPurchaseFailed   += order => Debug.LogWarning("Purchase failed: " + order.FailureReason);
        _controller.OnStoreDisconnected += desc => Debug.LogWarning("Store disconnected: " + desc.message);

        // Connect to the store. This must complete before fetching products or
        // starting purchases.
        try
        {
            await _controller.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError("Store connection failed: " + e.Message);
            return;
        }

        // Register the products your game sells. The storeSpecificId must match
        // the product identifier you set up in App Store Connect (for Apple) or
        // the Catappult console (for AppCoins).
        // See: https://docs.unity3d.com/Packages/com.unity.purchasing@5.4/manual/UnityIAPDefiningProducts.html
        _controller.FetchProducts(new List<ProductDefinition>
        {
            new ProductDefinition("gas", ProductType.Consumable),
        });
    }

    // Called once the store returns the product catalogue with live prices and
    // metadata. Cache the products you need for your buy buttons here.
    private void OnProductsFetched(List<Product> products)
    {
        foreach (var p in products)
            Debug.Log($"{p.definition.id}: {p.metadata.localizedTitle} @ {p.metadata.localizedPriceString}");

        _gasProduct = products.FirstOrDefault(p => p.definition.id == "gas");

        // Recover any purchases that were paid but not consumed. See the
        // "Handling unfinished purchases" section below.
        _controller.FetchPurchases();
    }

    // Called with any purchases from a previous session that were not yet
    // confirmed (consumed).
    private void OnPurchasesFetched(Orders orders)
    {
        foreach (var pending in orders.PendingOrders)
            ProcessPendingOrder(pending);
    }

    // Call this from your "Buy" button.
    public void BuyGas()
    {
        if (_gasProduct != null)
            _controller.PurchaseProduct(_gasProduct);
    }

    // Called when a purchase is authorised and waiting to be fulfilled.
    // This fires both for new purchases and for purchases recovered from a
    // previous session via FetchPurchases().
    private void OnPurchasePending(PendingOrder order)
    {
        ProcessPendingOrder(order);
    }

    private void ProcessPendingOrder(PendingOrder order)
    {
        string productId = order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id;

        // Grant the item to the player first.
        if (productId == "gas") AddGas();

        // Then confirm (consume) the order. For consumables, this allows the
        // product to be purchased again. For non-consumables it marks the
        // order as complete.
        _controller.ConfirmPurchase(order);
    }

    private void AddGas() { /* your game logic */ }
}
```

> ⚠️ **Always call `FetchPurchases()` on startup.** If the app is closed after payment but before `ConfirmPurchase` runs, the purchase stays unfinished. The AppCoins SDK refunds unfinished purchases after 24 hours. Recovering and confirming them on every launch guarantees users always receive what they paid for.

A fully runnable version of this flow is available at `Assets/Plugins/iOS/AppCoinsSDKPlugin/Samples/TrivialDriveIAP.cs`.

---

## Server-Side Receipt Verification

On `OnPurchasePending`, `order.Info.Receipt` contains a JSON receipt with the shape `{ "Store", "TransactionID", "Payload" }`. The `Payload` carries the AppCoins purchase record and its signature verification, which your backend can use to confirm the transaction is genuine before granting high-value items.

Confirming purchases client-side is acceptable during development, but **always add server-side validation before shipping to production** to prevent fraudulent purchases.

---

## Testing

### Enabling the Aptoide store in Xcode

To test the AppCoins store on a development build, you need to simulate alternative distribution. In Xcode:

1. In your target's **Build Settings**, search for **Marketplaces**.
2. Under **Deployment**, set **Marketplaces** to `com.aptoide.ios.store`.
3. Open your **scheme → Run → Options** and set **Distribution** to `com.aptoide.ios.store`.

See [Apple's documentation](https://developer.apple.com/documentation/appdistribution/distributing-your-app-on-an-alternative-marketplace#Test-your-app-during-development) for more detail.

### Toggling between Aptoide and Apple in one build

When using `AppCoinsStoreMode.Automatic`, you can switch which store is active at runtime via a deep link. Open the device browser and navigate to:

```
{bundleId}.iap://wallet.appcoins.io/default?value={true|false}
```

- `value=true` → enables the AppCoins store.
- `value=false` → disables it; `Automatic` falls back to Apple.

Replace `{bundleId}` with your app's Bundle ID (e.g. `com.example.mygame`).

### Sandbox

To simulate purchases without real payments, use the AppCoins sandbox environment: [Sandbox documentation](https://docs.connect.aptoide.com/docs/ios-sandbox-environment).

---

## Not on Unity IAP 5 yet?

Unity IAP v5 is our recommended integration path. That said, migrating an existing IAP implementation is not always practical in the short term.

If you are on Unity IAP 4.x (or not using Unity IAP at all), the previous version of the AppCoins plugin is still available and fully supported. Use the [legacy plugin release](https://github.com/Catappult/appcoins-sdk-ios-unity-plugin/releases) and its corresponding documentation until you are ready to move to v5.

> Note: Unity IAP 4.x and 5.x cannot coexist in the same project. When you are ready to upgrade, see Unity's [migration guide](https://docs.unity3d.com/Packages/com.unity.purchasing@5.4/manual/MigrationGuide.html).

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
