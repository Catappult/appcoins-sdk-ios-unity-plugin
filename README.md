# AppCoins Unity Plugin — iOS

The iOS Billing SDK is a simple solution to implement Aptoide billing. Its Unity Plugin registers AppCoins as a **Unity IAP v5 custom store**, so your game uses the standard Unity In-App Purchasing API (`StoreController`) and AppCoins backs the purchases on iOS alternative distribution. When the same game ships through the Apple App Store, Unity IAP transparently uses Apple's StoreKit instead — your purchasing code stays identical.

The SDK automatically handles transaction reporting to Apple for Core Technology Commission (CTC) calculation, removing this burden from developers.

> **Unity IAP v5 is the only supported integration model.** The previous bespoke `AppCoinsSDK.Instance` async API has been removed. Existing integrations that rely on it should remain on the older plugin release. See [Migrating from the legacy AppCoins API](#migrating-from-the-legacy-appcoins-api).

---

## Requirements

- Unity 2022.3 or later
- Unity In-App Purchasing (`com.unity.purchasing`) **5.4.0 or later**
- iOS 17.4 or higher (for Aptoide alternative distribution)

---

## Installation

### Step 1 — Unity In-App Purchasing

Choose the path that matches your current project state:

#### I don't have Unity IAP yet

1. Open **Window > Package Manager**.
2. Click the **+** button and select **Add package by name**.
3. Enter `com.unity.purchasing` and version `5.4.0`, then click **Add**.

Alternatively, open `Packages/manifest.json` and add the line manually:

```json
"com.unity.purchasing": "5.4.0"
```

Unity will resolve the package (and its dependencies) the next time the project opens.

#### I have Unity IAP 4.x and need to upgrade

Unity IAP v5 changed the API significantly — the `IStoreListener` / `ConfigurationBuilder` / `ProcessPurchase` model was replaced with `StoreController` and async events. Your existing purchasing code will need to be rewritten; this guide covers the v5 API.

To upgrade:

1. Open **Window > Package Manager**.
2. Find **In App Purchasing** in the **In Project** list.
3. Click **Update** and select version **5.4.0** (or the latest 5.x).
4. Resolve any compilation errors — see Unity's [IAP 5 upgrade guide](https://docs.unity3d.com/Packages/com.unity.purchasing@5.4/manual/StoreController.html) for the API changes.

#### I already have Unity IAP 5.4.0 or later

No action needed — proceed to Step 2.

---

### Step 2 — AppCoins Plugin

1. Download the latest `.unitypackage` from the [Releases page](https://github.com/Catappult/appcoins-sdk-ios-unity-plugin/releases).
2. In Unity, go to **Assets > Import Package > Custom Package**, select the `.unitypackage`, and import all files.

The plugin installs into `Assets/Plugins/iOS/AppCoinsSDKPlugin/`. It registers the AppCoins custom store **automatically** on iOS at startup — there is no manual registration step.

---

## Quick Start

The complete flow, from store selection to purchase confirmation:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using AppCoins.Unity;

public class Shop : MonoBehaviour
{
    private StoreController _controller;
    private Product _gasProduct;

    private async void Start()
    {
        // 1. Choose the store. Automatic picks Aptoide when running under
        //    alternative distribution, Apple App Store otherwise.
        await AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic);

        // 2. Get the StoreController and subscribe to events.
        _controller = UnityIAPServices.StoreController();
        _controller.OnProductsFetched   += OnProductsFetched;
        _controller.OnPurchasesFetched  += OnPurchasesFetched;
        _controller.OnPurchasePending   += OnPurchasePending;
        _controller.OnPurchaseFailed    += order => Debug.LogWarning($"Purchase failed: {order.FailureReason}");
        _controller.OnPurchaseConfirmed += order => Debug.Log("Purchase confirmed: " + order.Info.TransactionID);
        _controller.OnStoreDisconnected += desc  => Debug.LogWarning("Store disconnected: " + desc.message);

        // 3. Connect to the store.
        try
        {
            await _controller.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError("Store connection failed: " + e.Message);
            return;
        }

        // 4. Declare the products you sell.
        _controller.FetchProducts(new List<ProductDefinition>
        {
            new ProductDefinition("gas",      ProductType.Consumable),
            new ProductDefinition("coins_100", ProductType.Consumable),
        });
    }

    // Called once the product catalog has been fetched.
    private void OnProductsFetched(List<Product> products)
    {
        foreach (var p in products)
        {
            Debug.Log($"{p.definition.id}: {p.metadata.localizedTitle} @ {p.metadata.localizedPriceString}");
            if (p.definition.id == "gas")
                _gasProduct = p;
        }

        // Recover any purchases that were paid but not yet consumed
        // (e.g. the app crashed before ConfirmPurchase was called).
        _controller.FetchPurchases();
    }

    // Called with purchases recovered from a previous session.
    private void OnPurchasesFetched(Orders orders)
    {
        foreach (var pending in orders.PendingOrders)
            ProcessPendingOrder(pending);
    }

    // Triggers a purchase when the player taps "Buy Gas".
    public void BuyGas()
    {
        if (_gasProduct != null)
            _controller.PurchaseProduct(_gasProduct);
    }

    // Called when a purchase is authorized and waiting for your app to grant
    // the item and confirm (consume) the order.
    private void OnPurchasePending(PendingOrder order)
    {
        ProcessPendingOrder(order);
    }

    private void ProcessPendingOrder(PendingOrder order)
    {
        string productId = order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id;

        // Grant the item to the player.
        if (productId == "gas") AddGas();

        // Confirm (consume) the order so the consumable can be bought again.
        _controller.ConfirmPurchase(order);
    }

    private void AddGas() { /* your game logic */ }
}
```

A complete, runnable sample is available at `Assets/Plugins/iOS/AppCoinsSDKPlugin/Samples/TrivialDriveIAP.cs`.

---

## Choosing the Store

Call `AppCoinsIAP.ConfigureStoreAsync(mode)` **once**, before `StoreController.Connect()`. It sets which store backs purchases for the entire session.

| Mode | Behaviour |
|---|---|
| `AppCoinsStoreMode.Automatic` *(recommended)* | Calls AppCoins `isAvailable`: picks **Aptoide** under alternative distribution, **Apple** otherwise. |
| `AppCoinsStoreMode.Aptoide` | Always routes through the AppCoins store. |
| `AppCoinsStoreMode.Apple` | Always routes through Apple's built-in StoreKit. |

On non-iOS platforms (Android, Editor) this call is a no-op.

---

## Handling Unfinished Purchases on Launch

⚠️ **This step is mandatory.** If your app is closed after payment is collected but before `ConfirmPurchase` is called, the purchase is left unfinished. The AppCoins SDK will automatically refund unfinished purchases after 24 hours.

Always call `FetchPurchases()` after connecting and process every item in `orders.PendingOrders` exactly as you would a live purchase. The example above shows this pattern in `OnProductsFetched`.

---

## Server-Side Receipt Verification

On `OnPurchasePending`, `order.Info.Receipt` contains a JSON receipt:

```json
{
  "Store":         "AppCoinsAppStore",
  "TransactionID": "<orderUID>",
  "Payload":       "<JSON string>"
}
```

The `Payload` is itself a JSON string that carries the AppCoins purchase record and its signature verification:

```json
{
  "uid":                "...",
  "sku":                "gas",
  "state":              "ACKNOWLEDGED",
  "orderUid":           "...",
  "verificationResult": "verified",
  "verification": {
    "type":      "GOOGLE",
    "signature": "...",
    "data": {
      "packageName":    "com.example.game",
      "productId":      "gas",
      "purchaseToken":  "...",
      "purchaseTime":   1234567890
    }
  }
}
```

Forward the `Payload` (or the entire `Receipt`) to your backend, which can verify the signature against the AppCoins Remote Check API, or use the `purchaseToken` to call the Catappult validation endpoint directly.

If you do not yet have a backend, you can confirm purchases client-side during development, but **always add server-side validation before shipping to production**.

---

## Testing

### Enabling the Aptoide store in Xcode

To test the AppCoins store on a development build, you need to simulate alternative distribution. In Xcode:

1. In your target's **Build Settings**, search for **Marketplaces**.
2. Under **Deployment**, set **Marketplaces** (or **Alternative Distribution - Marketplaces**) to `com.aptoide.ios.store`.
3. Open your **scheme** → **Run** → **Options** tab → set **Distribution** to `com.aptoide.ios.store`.

For more detail see [Apple's documentation](https://developer.apple.com/documentation/appdistribution/distributing-your-app-on-an-alternative-marketplace#Test-your-app-during-development).

### Toggling between Aptoide and Apple in one build

The SDK provides a deep-link to flip `isAvailable` at runtime, which lets you test both billing systems in a single build when using `AppCoinsStoreMode.Automatic`. Open your device's browser and navigate to:

```
{bundleId}.iap://wallet.appcoins.io/default?value={true|false}
```

- Replace `{bundleId}` with your app's Bundle ID (e.g. `com.example.game`).
- `value=true` — enables the AppCoins store.
- `value=false` — disables it, so `Automatic` falls back to Apple.

### Sandbox environment

To simulate purchases without real payments, use the AppCoins sandbox. See [Sandbox documentation](https://docs.connect.aptoide.com/docs/ios-sandbox-environment).

---

## API Reference

### How AppCoins maps onto Unity IAP

| Unity IAP v5 | AppCoins behaviour |
|---|---|
| `ConfigureStoreAsync(Automatic)` | Calls `isAvailable` to pick the right store. |
| `StoreController.Connect()` | Initialises the AppCoins SDK. |
| `FetchProducts(defs)` | Fetches prices and metadata; maps AppCoins `Product` to Unity `Product`. |
| `PurchaseProduct(product)` | Triggers the AppCoins purchase sheet. |
| `OnPurchasePending` | Purchase authorized — grant the item and call `ConfirmPurchase`. |
| `OnPurchaseDeferred` | Payment authorized but not yet settled (e.g. parental approval). |
| `OnPurchaseFailed` | Purchase cancelled or failed — reason and details included. |
| `ConfirmPurchase(order)` | Consumes the purchase so it can be bought again. |
| `FetchPurchases()` | Returns unfinished purchases for startup recovery. |
| `CheckEntitlement(product)` | Checks whether the user has an unconsumed purchase for the product. |
| Deep-link / indirect purchase intents | Surfaced automatically through `OnPurchasePending`. |

---

## Migrating from the Legacy AppCoins API

Previous versions exposed a bespoke `AppCoins.AppCoinsSDK.Instance` async/await singleton (plus `AppCoinsPurchaseManager`). That public API has been **removed** in favour of Unity IAP v5.

| Legacy API | Unity IAP v5 equivalent |
|---|---|
| `AppCoinsSDK.Instance.IsAvailable()` | `AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic)` |
| `AppCoinsSDK.Instance.GetProducts(skus)` | `StoreController.FetchProducts(defs)` → `OnProductsFetched` |
| `AppCoinsSDK.Instance.Purchase(sku, payload)` | `StoreController.PurchaseProduct(product)` → `OnPurchasePending` |
| `AppCoinsSDK.Instance.ConsumePurchase(sku)` | `StoreController.ConfirmPurchase(order)` → `OnPurchaseConfirmed` |
| `AppCoinsSDK.Instance.GetAllPurchases()` / `GetUnfinishedPurchases()` | `StoreController.FetchPurchases()` → `OnPurchasesFetched` |
| `AppCoinsSDK.Instance.GetLatestPurchase(sku)` | `StoreController.CheckEntitlement(product)` → `OnCheckEntitlement` |
| `AppCoinsPurchaseManager.OnPurchaseUpdated` | `StoreController.OnPurchasePending` (intents surfaced automatically) |
| Purchase verification signature | `order.Info.Receipt` → parse `Payload` → `verification.signature` |

If you must keep the legacy async API, stay on the previous plugin release. The two models cannot be mixed in a single build.
