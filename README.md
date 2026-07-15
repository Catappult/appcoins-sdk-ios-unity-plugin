# AppCoins Unity Plugin — iOS

The AppCoins Unity Plugin integrates Aptoide billing into Unity games by registering AppCoins as a **Unity IAP v5 custom store**. Unity IAP is the standard purchasing layer for Unity — a single API that abstracts StoreKit, Google Play Billing, and other backends so your purchasing code works across every platform unchanged.

With this plugin, purchases are routed through AppCoins when running under Aptoide alternative distribution, and fall back to StoreKit when running through the Apple App Store. Apple's Core Technology Commission (CTC) transaction reporting is handled automatically.

If you already have Unity IAP 5 set up, adding AppCoins takes **one line of code**.

---

## Quick Start

**1.** Download the latest `.unitypackage` from the [Releases page](https://github.com/Catappult/appcoins-sdk-ios-unity-plugin/releases) and import it via **Assets > Import Package > Custom Package**.

**2.** Add one line before your existing `Connect()` call:

```csharp
using AppCoins.Unity;

// Add this before StoreController.Connect()
await AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic);
```

AppCoins is now active alongside your existing Unity IAP integration.

Use `Automatic` if you are distributing the same build on both Aptoide and the Apple App Store — the SDK determines at runtime which store the app was installed from and routes purchases accordingly. Use `AppCoinsStoreMode.Aptoide` if the build is exclusively for Aptoide, or `AppCoinsStoreMode.Apple` if it is exclusively for the Apple App Store.

---

## Setting up Unity IAP 5 from scratch

The Unity IAP v5 store flow has four stages: connect, fetch products, purchase, and confirm. The [official Unity IAP documentation](https://docs.unity3d.com/Packages/com.unity.purchasing@5.4/manual/index.html) covers the package in depth. Below is a complete working integration with AppCoins included.

### Requirements

- Unity 2022.3 or later
- Unity In-App Purchasing (`com.unity.purchasing`) 5.4.0 or later
- iOS 17.4 or higher

### Installing Unity IAP 5

1. Open **Window > Package Manager**.
2. Click **+** and select **Add package by name**.
3. Enter `com.unity.purchasing` as the name and `5.4.0` as the version, then click **Add**.

Unity resolves the package and its dependencies automatically. Confirm by checking that `com.unity.purchasing` appears in `Packages/manifest.json`.

### Full implementation

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

        // StoreController is the central Unity IAP object. It manages the store
        // connection, the product catalogue, and all purchase callbacks.
        // See: https://docs.unity3d.com/Packages/com.unity.purchasing@5.4/manual/StoreController.html
        _controller = UnityIAPServices.StoreController();
        _controller.OnProductsFetched   += OnProductsFetched;
        _controller.OnPurchasesFetched  += OnPurchasesFetched;
        _controller.OnPurchasePending   += OnPurchasePending;
        _controller.OnPurchaseFailed    += order => Debug.LogWarning("Purchase failed: " + order.FailureReason);
        _controller.OnStoreDisconnected += desc  => Debug.LogWarning("Store disconnected: " + desc.message);

        try
        {
            await _controller.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError("Store connection failed: " + e.Message);
            return;
        }

        // Product identifiers must match what is configured in App Store Connect
        // (for Apple) or the Catappult console (for AppCoins).
        // See: https://docs.unity3d.com/Packages/com.unity.purchasing@5.4/manual/UnityIAPDefiningProducts.html
        _controller.FetchProducts(new List<ProductDefinition>
        {
            new ProductDefinition("gas", ProductType.Consumable),
        });
    }

    // Fired once the store returns the live product catalogue. Cache the
    // products you need for purchase buttons here.
    private void OnProductsFetched(List<Product> products)
    {
        foreach (var p in products)
            Debug.Log($"{p.definition.id}: {p.metadata.localizedTitle} @ {p.metadata.localizedPriceString}");

        _gasProduct = products.FirstOrDefault(p => p.definition.id == "gas");

        // Recover purchases that were paid but not yet consumed (e.g. the app
        // was killed before ConfirmPurchase ran).
        _controller.FetchPurchases();
    }

    // Fired with unconfirmed purchases from previous sessions.
    private void OnPurchasesFetched(Orders orders)
    {
        foreach (var pending in orders.PendingOrders)
            ProcessPendingOrder(pending);
    }

    public void BuyGas()
    {
        if (_gasProduct != null)
            _controller.PurchaseProduct(_gasProduct);
    }

    // Fired when a purchase is authorised and waiting to be fulfilled. This
    // covers both new purchases and purchases recovered via FetchPurchases().
    private void OnPurchasePending(PendingOrder order)
    {
        ProcessPendingOrder(order);
    }

    private void ProcessPendingOrder(PendingOrder order)
    {
        string productId = order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id;

        // Grant the item before confirming.
        if (productId == "gas") AddGas();

        // ConfirmPurchase consumes the order, allowing consumables to be
        // purchased again and marking non-consumables as complete.
        _controller.ConfirmPurchase(order);
    }

    private void AddGas() { /* your game logic */ }
}
```

> ⚠️ **Always call `FetchPurchases()` on startup.** If the app is terminated after payment but before `ConfirmPurchase` runs, the purchase stays unfinished. The AppCoins SDK refunds unfinished purchases after 24 hours. Processing them on every launch ensures users always receive what they paid for.

A fully runnable version of this flow is available at `Assets/Plugins/iOS/AppCoinsSDKPlugin/Samples/TrivialDriveIAP.cs`.

---

## Server-Side Receipt Verification

On `OnPurchasePending`, `order.Info.Receipt` is a JSON receipt with the shape `{ "Store", "TransactionID", "Payload" }`. The `Payload` carries the AppCoins purchase record and its signature, which your backend can use to verify the transaction before granting items.

Client-side confirmation is fine for most use cases. For high-value items, server-side validation is strongly recommended to prevent fraudulent purchases.

---

## Testing

### Enabling the Aptoide store in Xcode

To test the AppCoins store on a development build, simulate alternative distribution in Xcode:

1. In your target's **Build Settings**, search for **Marketplaces**.
2. Under **Deployment**, set **Marketplaces** to `com.aptoide.ios.store`.
3. Open your **scheme → Run → Options** and set **Distribution** to `com.aptoide.ios.store`.

See [Apple's documentation](https://developer.apple.com/documentation/appdistribution/distributing-your-app-on-an-alternative-marketplace#Test-your-app-during-development) for full details.

### Toggling between Aptoide and Apple in one build

With `AppCoinsStoreMode.Automatic`, you can switch the active store at runtime via a deep link. Open the device browser and navigate to:

```
{bundleId}.iap://wallet.appcoins.io/default?value={true|false}
```

- `value=true` enables the AppCoins store.
- `value=false` disables it; `Automatic` falls back to Apple.

Replace `{bundleId}` with your app's Bundle ID (e.g. `com.example.mygame`).

### Sandbox

Simulate purchases without real payments using the AppCoins sandbox: [Sandbox documentation](https://docs.connect.aptoide.com/docs/ios-sandbox-environment).

---

## Not on Unity IAP 5 yet?

Unity IAP v5 is the recommended integration path. Migrating an existing IAP implementation is not always practical in the short term, though. If you are on Unity IAP 4.x or not using Unity IAP at all, the previous version of the AppCoins plugin remains fully supported. Use the [legacy plugin release](https://github.com/Catappult/appcoins-sdk-ios-unity-plugin/releases) and its documentation until you are ready to move to v5.

Unity IAP 4.x and 5.x cannot coexist in the same project. When you are ready to upgrade, refer to Unity's [IAP migration guide](https://docs.unity3d.com/Packages/com.unity.purchasing@5.4/manual/MigrationGuide.html).

---

## Migrating from the Legacy AppCoins API

If you were previously using the `AppCoinsSDK.Instance` async API, the table below maps each call to its Unity IAP v5 equivalent:

| Legacy API | Unity IAP v5 equivalent |
|---|---|
| `AppCoinsSDK.Instance.IsAvailable()` | `AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic)` |
| `AppCoinsSDK.Instance.GetProducts(skus)` | `StoreController.FetchProducts(defs)` → `OnProductsFetched` |
| `AppCoinsSDK.Instance.Purchase(sku)` | `StoreController.PurchaseProduct(product)` → `OnPurchasePending` |
| `AppCoinsSDK.Instance.ConsumePurchase(sku)` | `StoreController.ConfirmPurchase(order)` → `OnPurchaseConfirmed` |
| `AppCoinsSDK.Instance.GetAllPurchases()` / `GetUnfinishedPurchases()` | `StoreController.FetchPurchases()` → `OnPurchasesFetched` |
| `AppCoinsSDK.Instance.GetLatestPurchase(sku)` | `StoreController.CheckEntitlement(product)` → `OnCheckEntitlement` |
| `AppCoinsPurchaseManager.OnPurchaseUpdated` | `StoreController.OnPurchasePending` (surfaced automatically) |
