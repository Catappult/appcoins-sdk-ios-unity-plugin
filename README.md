The iOS Billing SDK is a simple solution to implement Aptoide billing. Its Unity Plugin registers AppCoins as a **Unity IAP v5 custom store**, so your game uses the standard Unity In-App Purchasing API (`StoreController`) and AppCoins backs the purchases on iOS alternative distribution. When the same game ships through the Apple App Store, Unity IAP transparently uses Apple's StoreKit instead — your purchasing code stays identical.

The SDK automatically handles transaction reporting to Apple for Core Technology Commission (CTC) calculation, removing this burden from developers. It includes intelligent logic for reporting purchases, refunds, and other transaction events, with region-aware processing that distinguishes which regions require CTC reporting and which do not.

> **Unity IAP v5 is the supported integration model.** New integrations must use Unity IAP v5 as described below. The previous bespoke `AppCoinsSDK.Instance` async API has been removed from this version; existing integrations that rely on it should remain on the older plugin release. See [Migrating from the legacy AppCoins API](#migrating-from-the-legacy-appcoins-api).

## In Summary

The billing flow in your application with the Plugin is as follows:

1. Add the AppCoins Unity Plugin and the Unity In-App Purchasing package.
2. Choose the backing store with a single `AppCoinsIAP.ConfigureStoreAsync(...)` call.
3. Use the standard Unity IAP API to connect, fetch products, and purchase.
4. On a pending order, validate the receipt and confirm (consume) the order.
5. Grant the product to the user.

## Requirements

1. Unity 2022.3 or later (required by Unity IAP v5).
2. Unity In-App Purchasing (`com.unity.purchasing`) 5.4.0 or later.
3. iOS 17.4 or higher.

## Step-by-Step Guide

### Setup

1. **Add the Unity In-App Purchasing package.** It is already declared in `Packages/manifest.json`:

   ```json
   "com.unity.purchasing": "5.4.0"
   ```

   Unity will resolve it (and its dependencies) automatically when the project opens.

2. **Add the AppCoins Unity Plugin.** Add the plugin from the latest release on <https://github.com/Catappult/appcoins-sdk-ios-unity-plugin> to your `Assets` folder. The AppCoins custom store registers itself automatically on iOS at startup — there is no manual registration step.

### Namespace

The public integration surface lives in the `AppCoins.Unity` namespace:

```csharp
using AppCoins.Unity;
```

Everything else you use comes from Unity IAP's `UnityEngine.Purchasing` namespace.

### Choosing the store (Automatic / Aptoide / Apple)

Before using Unity IAP's `StoreController`, call `AppCoinsIAP.ConfigureStoreAsync(mode)` **once**. It selects which store backs purchases and sets it as Unity IAP's default. The mode is up to you:

- **`AppCoinsStoreMode.Automatic`** *(recommended)* — uses the AppCoins `isAvailable` check to pick Aptoide when the app is running under alternative distribution (iOS 17.4+), and the Apple App Store otherwise.
- **`AppCoinsStoreMode.Aptoide`** — always use the AppCoins (Aptoide) store.
- **`AppCoinsStoreMode.Apple`** — always use the built-in Apple App Store.

```csharp
using AppCoins.Unity;
using UnityEngine.Purchasing;

// Automatic: isAvailable decides between Aptoide and Apple.
string store = await AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic);

// ...or force one of the two:
// await AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Aptoide);
// await AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Apple);
```

On non-iOS platforms this call is a no-op that leaves Unity's normal platform default in place, so nothing is disturbed.

### Implementation

From here on it is standard Unity IAP v5. Create a `StoreController`, subscribe to its events, and connect.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using AppCoins.Unity;

public class Shop : MonoBehaviour
{
    private static readonly string[] Skus = { "gas", "coins_100" };
    private StoreController _controller;

    private async void Start()
    {
        await AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic);

        _controller = UnityIAPServices.StoreController();
        _controller.OnStoreConnected    += OnConnected;
        _controller.OnProductsFetched   += OnProductsFetched;
        _controller.OnPurchasePending   += OnPurchasePending;
        _controller.OnPurchaseConfirmed += order => Debug.Log("Confirmed: " + order.Info.TransactionID);
        _controller.OnPurchaseFailed    += f => Debug.Log($"Failed: {f.FailureReason} - {f.Details}");
        _controller.OnPurchaseDeferred  += _ => Debug.Log("Payment pending.");

        await _controller.Connect();
    }

    private void OnConnected()
    {
        var defs = new List<ProductDefinition>();
        foreach (var sku in Skus)
            defs.Add(new ProductDefinition(sku, ProductType.Consumable));

        _controller.FetchProducts(defs);
        _controller.FetchPurchases(); // recover unfinished purchases on launch
    }

    private void OnProductsFetched(List<Product> products)
    {
        foreach (var p in products)
            Debug.Log($"{p.definition.id}: {p.metadata.localizedTitle} @ {p.metadata.localizedPriceString}");
    }

    public void Buy(string sku)
    {
        var product = _controller.GetProductById(sku);
        if (product != null) _controller.PurchaseProduct(product);
    }

    private void OnPurchasePending(PendingOrder order)
    {
        // order.Info.Receipt carries the AppCoins signature verification.
        GrantItem(order);
        _controller.ConfirmPurchase(order); // consume
    }

    private void GrantItem(Order order)
    {
        foreach (var item in order.CartOrdered.Items())
            Debug.Log("Granting " + item.Product.definition.id);
    }
}
```

A complete, runnable version of this flow is provided in the sample at
`Assets/Plugins/iOS/AppCoinsSDKPlugin/Samples/TrivialDriveIAP.cs`.

#### How AppCoins maps onto Unity IAP

| Unity IAP v5 | AppCoins behaviour |
|---|---|
| `StoreController.Connect()` | `AppcSDK.initialize` + `isAvailable`; failure maps to a Unity store-connection failure. |
| `FetchProducts(...)` | Maps AppCoins `Product` to Unity `Product` (price value/label/currency/symbol, title, description). |
| `PurchaseProduct(...)` | `Product.purchase`. Success → `OnPurchasePending`; pending → `OnPurchaseDeferred`; user-cancel/failure → `OnPurchaseFailed`. |
| `ConfirmPurchase(pendingOrder)` | `Purchase.finish` (consume) → `OnPurchaseConfirmed`. |
| `FetchPurchases()` | Backed by `Purchase.unfinished` + `Purchase.all` (unfinished-purchase recovery). |
| `CheckEntitlement(product)` | Backed by `Purchase.latest`. |
| Deep-link / indirect purchase intents (`Purchase.updates`) | Surfaced through the standard `OnPurchasePending` flow. |

#### Receipt / server-side verification

On a pending order, `order.Info.Receipt` is a JSON "unified receipt" with the shape `{ "Store", "TransactionID", "Payload" }`. The `Payload` is a JSON string carrying the AppCoins purchase and its signature verification (`type`, `signature`, `data`, `verificationResult`). Forward the `Payload` to your backend to validate the signature through Remote Check.

### Handling unfinished purchases on launch (CRITICAL)

⚠️ **CRITICAL:** You **MUST** recover unfinished purchases every time your application starts. Failing to do so will result in users not receiving items they've already paid for, and purchases will be automatically refunded after 24 hours if not consumed.

Call `_controller.FetchPurchases()` after connecting. Any purchase that was paid for but not consumed is delivered as a `PendingOrder`; grant the item and call `ConfirmPurchase(order)` to consume it. This is shown in the sample above.

### Testing

To test the SDK integration during development, you'll need to set the installation source for development builds, simulating that the app is being distributed through Aptoide. This action enables the SDK's `isAvailable` method (and therefore the Aptoide store in `Automatic` mode). Follow these steps in Xcode:

1. In your target build settings, search for "Marketplaces".
2. Under "Deployment", set the key "Marketplaces" or "Alternative Distribution - Marketplaces" to "com.aptoide.ios.store".
3. In your scheme, go to the "Run" tab, then the "Options" tab. In the "Distribution" dropdown, select "com.aptoide.ios.store".

   For more information, please refer to Apple's official documentation: <https://developer.apple.com/documentation/appdistribution/distributing-your-app-on-an-alternative-marketplace#Test-your-app-during-development>

### Testing both billing systems in one build

To test both **Apple Billing** and **Aptoide Billing** within a single build, the **AppCoins SDK** includes a deep link mechanism that toggles the SDK's `isAvailable` method between `true` and `false`. With `AppCoinsStoreMode.Automatic`, this lets you switch between the AppCoins store (when available) and Apple Billing (when unavailable).

Open your device's browser and enter:

```
{domain}.iap://wallet.appcoins.io/default?value={value}
```

Where:

- `domain` – The Bundle ID of your application.
- `value`
  - `true` → Enables the AppCoins SDK for testing.
  - `false` → Disables the AppCoins SDK, allowing Apple Billing to be tested instead.

### Sandbox

To verify the successful setup of your billing integration, we offer a sandbox environment where you can simulate purchases. Documentation on how to use this environment can be found at: [Sandbox](https://docs.connect.aptoide.com/docs/ios-sandbox-environment)

## Migrating from the legacy AppCoins API

Previous versions exposed a bespoke `AppCoins.AppCoinsSDK.Instance` async/await singleton (plus `AppCoinsPurchaseManager`). That public API has been **removed** in favour of Unity IAP v5. The underlying native Swift/Obj-C++ bridge is unchanged — only the C# surface changed.

| Legacy API | Unity IAP v5 equivalent |
|---|---|
| `AppCoinsSDK.Instance.IsAvailable()` | `AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic)` + `StoreController.Connect()` |
| `AppCoinsSDK.Instance.GetProducts(skus)` | `StoreController.FetchProducts(defs)` → `OnProductsFetched` |
| `AppCoinsSDK.Instance.Purchase(sku, payload)` | `StoreController.PurchaseProduct(product)` → `OnPurchasePending` |
| `AppCoinsSDK.Instance.ConsumePurchase(sku)` | `StoreController.ConfirmPurchase(pendingOrder)` → `OnPurchaseConfirmed` |
| `AppCoinsSDK.Instance.GetAllPurchases()` / `GetUnfinishedPurchases()` | `StoreController.FetchPurchases()` → `OnPurchasesFetched` |
| `AppCoinsSDK.Instance.GetLatestPurchase(sku)` | `StoreController.CheckEntitlement(product)` → `OnCheckEntitlement` |
| `AppCoinsPurchaseManager.OnPurchaseUpdated` (purchase intents) | `StoreController.OnPurchasePending` (intents are surfaced automatically) |
| Purchase verification signature | `order.Info.Receipt` → `Payload` → `verification.signature` |

If you must keep the legacy async API, stay on the previous plugin release; the two models are not intended to be mixed in a single build.
