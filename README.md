The iOS Billing SDK is a simple solution to implement Aptoide billing. Its Unity Plugin provides a simple interface for Unity games to communicate with the SDK. It consists of a Billing client that allows you to get your products from Aptoide Connect and process the purchase of those items.

## In Summary

The billing flow in your application with the Plugin is as follows:

1. Add the AppCoins Unity Plugin;
2. Query your In-App Products;
3. User wants to purchase a product;
4. Application starts the purchase and the Plugin handles it, returning the purchase status and validation data on completion;
5. Application gives the product to the user.

## Requirements

1. Unity 2019.4 or later.
2. iOS 17.4 or higher.

## Step-by-Step Guide

### Setup

1. **Add AppCoins Unity Plugin**  
   In Unity, add the plugin from the latest release available on the repository <https://github.com/Catappult/appcoins-sdk-ios-unity-plugin> to your Assets folder.

### Implementation

Now that you have the Plugin set-up you can start making use of its functionalities.

1. **Check AppCoins Billing Availability**  
   
   The AppCoins Billing will only be available on devices with an iOS version equal to or higher than 17.4 and only if the application was not installed through the Apple App St. Therefore, before attempting any purchase, you should check if the SDK is available by calling `AppCoinsSDK.Instance.IsAvailable()`.

   ```csharp
   var isAvailable = await AppCoinsSDK.Instance.IsAvailable();

   if (isAvailable) 
   {
     // make purchase
   }
   ```
2. **Query In-App Products**
   
   You should start by getting the In-App Products you want to make available to the user. This method can either return all of your Catappult In-App Products or a specific list.

   1. `AppCoinsSDK.Instance.GetProducts()`

      Returns all application Catappult In-App Products:

      ```csharp
      var productsResult = await AppCoinsSDK.Instance.GetProducts();

      if (productsResult.IsSuccess)
      {
          var products = productsResult.Value;
          // Process products
      }
      else
      {
          Debug.Log("Error: " + productsResult.Error);
      }
      ```

   2. `AppCoinsSDK.Instance.GetProducts(skus)`

      Returns a specific list of Catappult In-App Products:

      ```csharp
      var productsResult = await AppCoinsSDK.Instance.GetProducts(new string[] { "coins_100", "gas" });

      if (productsResult.IsSuccess)
      {
          var products = productsResult.Value;
          // Process products
      }
      else
      {
          Debug.Log("Error: " + productsResult.Error);
      }
      ```

   > ⚠️ **Warning:** You will only be able to query your In-App Products once your application is reviewed and approved on Aptoide Connect.
   
3. **Purchase In-App Product**
   
   To purchase an In-App Product you must call the function `AppCoinsSDK.Instance.Purchase(sku, payload)`. The Plugin will handle all of the purchase logic for you and it will return you on completion the result of the purchase. This result is an `AppCoinsSDKPurchaseResult` object with the following properties:

   1. `State`: String - The purchase state (`AppCoinsSDK.PURCHASE_STATE_SUCCESS`, `AppCoinsSDK.PURCHASE_STATE_PENDING`, `AppCoinsSDK.PURCHASE_STATE_USER_CANCELLED`, `AppCoinsSDK.PURCHASE_STATE_FAILED`)
   2. `Value`: Object containing:
      - `VerificationResult`: String - The verification result (`AppCoinsSDK.PURCHASE_VERIFICATION_STATE_VERIFIED`, `AppCoinsSDK.PURCHASE_VERIFICATION_STATE_UNVERIFIED`)
      - `Purchase`: Purchase object
      - `VerificationError`: AppCoinsSDKError (only present if verification fails)
   3. `Error`: AppCoinsSDKError - Error details (only present if state is `FAILED`)

   In case of success the application will verify the transaction's signature locally. After this verification you should handle its result:

   1. If the purchase is verified you should consume the item and give it to the user.
   2. If it is not verified you need to make a decision based on your business logic, you either still consume the item and give it to the user, or otherwise the purchase will not be acknowledged and we will refund the user in 24 hours.

   In case of failure you can deal with different types of errors.

   You can also pass a Payload to the purchase method in order to associate some sort of information with a specific purchase. You can use this for example to associate a specific user with a Purchase: `AppCoinsSDK.Instance.Purchase("gas", "User123")`.
   <br/>

   ```csharp
   var purchaseResult = await AppCoinsSDK.Instance.Purchase("gas", "User123");

   switch (purchaseResult.State)
   {
       case AppCoinsSDK.PURCHASE_STATE_SUCCESS:
           switch (purchaseResult.Value.VerificationResult)
           {
               case AppCoinsSDK.PURCHASE_VERIFICATION_STATE_VERIFIED:
                   // Consume the item and give it to the user
                   var consumeResult = await AppCoinsSDK.Instance.ConsumePurchase(purchaseResult.Value.Purchase.Sku);

                   if (consumeResult.IsSuccess)
                   {
                       Debug.Log("Purchase consumed successfully");
                   }
                   else
                   {
                       Debug.Log("Error consuming purchase: " + consumeResult.Error);
                   }
                   break;

               case AppCoinsSDK.PURCHASE_VERIFICATION_STATE_UNVERIFIED:
                   // Handle unverified purchase according to your game logic
                   break;
           }
           break;

       case AppCoinsSDK.PURCHASE_STATE_PENDING:
           // Handle pending purchase according to your game logic
           Debug.Log("Purchase is pending.");
           break;

       case AppCoinsSDK.PURCHASE_STATE_USER_CANCELLED:
           // Handle cancelled purchase according to your game logic
           Debug.Log("Purchase was cancelled.");
           break;

       case AppCoinsSDK.PURCHASE_STATE_FAILED:
           // Handle failed purchase according to your game logic
           Debug.Log("Purchase failed with error: " + purchaseResult.Error);
           break;
   }
   ```

4. **Handle Unfinished Purchases on App Launch (CRITICAL)**

   ⚠️ **CRITICAL:** You **MUST** query and consume unfinished purchases every time your application starts. Failing to do so will result in users not receiving items they've already paid for, and purchases will be automatically refunded after 24 hours if not consumed.

   **What are Unfinished Purchases?**

   Unfinished purchases are transactions that have been paid for but not yet consumed by your application. This can happen if:
   - The app was closed or crashed during a purchase
   - The user force-quit the app before the purchase was processed
   - A network error occurred during purchase completion

   **Why This is Critical:**
   - Users have already paid for these items
   - If not consumed within 24 hours, purchases are automatically refunded
   - Users expect to receive their purchased items immediately upon reopening the app

   **Implementation:**

   Add this code to your application's startup logic (e.g., in your main scene's `Start()` or `Awake()` method):

   ```csharp
   private async void Start()
   {
       // Check if AppCoins Billing is available
       var isAvailable = await AppCoinsSDK.Instance.IsAvailable();

       if (!isAvailable)
       {
           return;
       }

       // Query and consume unfinished purchases
       var unfinishedPurchasesResult = await AppCoinsSDK.Instance.GetUnfinishedPurchases();

       if (unfinishedPurchasesResult.IsSuccess)
       {
           var purchases = unfinishedPurchasesResult.Value;

           foreach (var purchase in purchases)
           {
               // Give the item to the user
               GiveItemToUser(purchase.Sku);

               // Consume the purchase
               var consumeResult = await AppCoinsSDK.Instance.ConsumePurchase(purchase.Sku);

               if (consumeResult.IsSuccess)
               {
                   Debug.Log($"Unfinished purchase consumed successfully: {purchase.Sku}");
               }
               else
               {
                   Debug.Log($"Error consuming purchase: {consumeResult.Error}");
               }
           }
       }
       else
       {
           Debug.Log("Error querying unfinished purchases: " + unfinishedPurchasesResult.Error);
       }
   }

   private void GiveItemToUser(string sku)
   {
       // Your logic to grant the purchased item to the user
       Debug.Log($"Giving item to user: {sku}");
   }
   ```

5. **Handle Indirect Purchases**

   In addition to standard In-App Purchases, the AppCoins SDK supports **Indirect In-App Purchases**. These are purchases that users do not initiate directly through a Buy action within the application. Common use cases include:

   1. Purchasing an item directly from a catalog of In-App Products in the Aptoide Store.
   2. Buying an item through a web link.

   The `AppCoinsPurchaseManager.OnPurchaseUpdated` Unity Action allows developers to manage these purchase intents. This event continuously streams purchase intent updates, ensuring real-time transaction synchronization.

   The event returns a `PurchaseIntent` object containing:
   - `ID`: String - Unique identifier for the intent
   - `Product`: Product - The product the user wants to purchase
   - `Timestamp`: String - When the intent was created

   When you receive a `PurchaseIntent`, you must either **confirm** it using `AppCoinsSDK.Instance.ConfirmPurchaseIntent(payload)` to complete the purchase, or **reject** it using `AppCoinsSDK.Instance.RejectPurchaseIntent()` to cancel. Confirming the intent returns an `AppCoinsSDKPurchaseResult` that should be handled the same way as a standard purchase.

   To properly handle purchase intents, subscribe to the event within a singleton class, ensuring it remains active for the application's lifecycle.

   **Note:** You can also manually check for pending purchase intents using `AppCoinsSDK.Instance.GetPurchaseIntent()`. This is useful when the user signs in or when your app becomes active, to ensure no pending intents are missed.
   <br/>

   ```csharp
   private void Awake()
   {
       // Singleton enforcement
       if (Instance != null && Instance != this)
       {
           Destroy(gameObject);  // Destroy duplicate instances
           return;
       }

       Instance = this;
       DontDestroyOnLoad(gameObject); // Persist across scenes

       // Subscribe to purchase intent updates
       AppCoinsPurchaseManager.OnPurchaseUpdated += HandlePurchaseIntent;
   }

   private async void HandlePurchaseIntent(PurchaseIntent purchaseIntent)
   {
       Debug.Log($"Received purchase intent for: {purchaseIntent.Product.Title}");

       // Confirm the purchase intent to complete the transaction
       var purchaseResult = await AppCoinsSDK.Instance.ConfirmPurchaseIntent("User123");

       // Handle the purchase result the same way as a standard purchase
       switch (purchaseResult.State)
       {
           case AppCoinsSDK.PURCHASE_STATE_SUCCESS:
               switch (purchaseResult.Value.VerificationResult)
               {
                   case AppCoinsSDK.PURCHASE_VERIFICATION_STATE_VERIFIED:
                       var consumeResult = await AppCoinsSDK.Instance.ConsumePurchase(purchaseResult.Value.Purchase.Sku);

                       if (consumeResult.IsSuccess)
                       {
                           Debug.Log("Purchase consumed successfully");
                       }
                       else
                       {
                           Debug.Log("Error consuming purchase: " + consumeResult.Error);
                       }
                       break;

                   case AppCoinsSDK.PURCHASE_VERIFICATION_STATE_UNVERIFIED:
                       // Handle unverified purchase according to your game logic
                       break;
               }
               break;

           case AppCoinsSDK.PURCHASE_STATE_USER_CANCELLED:
               Debug.Log("Purchase was cancelled.");
               break;

           case AppCoinsSDK.PURCHASE_STATE_FAILED:
               Debug.Log("Purchase failed with error: " + purchaseResult.Error);
               break;
       }

       // Alternatively, reject the purchase intent to cancel:
       // AppCoinsSDK.Instance.RejectPurchaseIntent();
   }
   ```

6. **Query Purchases**

   You can query the user's purchases by using one of the following methods:

   1. `AppCoinsSDK.Instance.GetAllPurchases()`

      This method returns all purchases that the user has performed in your application.

      ```csharp
      var purchasesResult = await AppCoinsSDK.Instance.GetAllPurchases();

      if (purchasesResult.IsSuccess)
      {
          var purchases = purchasesResult.Value;
          // Process purchases
      }
      else
      {
          Debug.Log("Error: " + purchasesResult.Error);
      }
      ```

   2. `AppCoinsSDK.Instance.GetLatestPurchase(string sku)`

      This method returns the latest user purchase for a specific In-App Product. Returns `null` if no purchase is found.

      ```csharp
      var latestPurchaseResult = await AppCoinsSDK.Instance.GetLatestPurchase("gas");

      if (latestPurchaseResult.IsSuccess)
      {
          if (latestPurchaseResult.Value != null)
          {
              var purchase = latestPurchaseResult.Value;
              // Process purchase
          }
          else
          {
              Debug.Log("No latest purchase found for this SKU");
          }
      }
      else
      {
          Debug.Log("Error: " + latestPurchaseResult.Error);
      }
      ```

   3. `AppCoinsSDK.Instance.GetUnfinishedPurchases()`

      This method returns all of the user's unfinished purchases in the application. An unfinished purchase is any purchase that has neither been acknowledged (verified by the SDK) nor consumed. You can use this method for consuming any unfinished purchases.

      ```csharp
      var unfinishedPurchasesResult = await AppCoinsSDK.Instance.GetUnfinishedPurchases();

      if (unfinishedPurchasesResult.IsSuccess)
      {
          var purchases = unfinishedPurchasesResult.Value;

          foreach (var purchase in purchases)
          {
              var consumeResult = await AppCoinsSDK.Instance.ConsumePurchase(purchase.Sku);

              if (consumeResult.IsSuccess)
              {
                  Debug.Log("Unfinished purchase consumed successfully");
              }
              else
              {
                  Debug.Log("Error consuming purchase: " + consumeResult.Error);
              }
          }
      }
      else
      {
          Debug.Log("Error: " + unfinishedPurchasesResult.Error);
      }
      ```

### Testing

To test the SDK integration during development, you'll need to set the installation source for development builds, simulating that the app is being distributed through Aptoide. This action will enable the SDK's `isAvailable` method.  
Follow these steps in Xcode:

1. In your target build settings, search for "Marketplaces".
2. Under "Deployment", set the key "Marketplaces" or "Alternative Distribution - Marketplaces" to "com.aptoide.ios.store".

   ![Screenshot 2024-05-16 at 09 54 11](https://github.com/Catappult/appcoins-sdk-ios-unity-plugin/assets/78313327/6ed9e0c8-98f4-4001-9d0b-31ee3fd8a5a5)
3. In your scheme, go to the "Run" tab, then navigate to the "Options" tab. In the "Distribution" dropdown, select "com.aptoide.ios.store".

   ![Screenshot 2024-05-16 at 09 43 48](https://github.com/Catappult/appcoins-sdk-ios-unity-plugin/assets/78313327/ae735ba2-d155-4ea6-a47e-4bab3eaf97ea)

   For more information, please refer to Apple's official documentation: <https://developer.apple.com/documentation/appdistribution/distributing-your-app-on-an-alternative-marketplace#Test-your-app-during-development>

### Testing Both Billing Systems in One Build

To facilitate testing both **Apple Billing** and **Aptoide Billing** within a single build – without the need to generate separate versions of your application – the **AppCoins SDK** includes a deep link mechanism that toggles the SDK’s `isAvailable` method between `true` and `false`. This allows you to seamlessly switch between testing the AppCoins SDK (when available) and Apple Billing (when unavailable).

To enable or disable the AppCoins SDK, open your device’s browser and enter the following URL:

```
{domain}.iap://wallet.appcoins.io/default?value={value}
```

Where:

- `domain` – The Bundle ID of your application.
- `value` 
  - `true` → Enables the AppCoins SDK for testing.
  - `false` → Disables the AppCoins SDK, allowing Apple Billing to be tested instead.

### Sandbox Testing

To verify the successful setup of your billing integration, we offer a sandbox environment where you can simulate purchases and ensure that your clients can smoothly purchase your products. Documentation on how to use this environment can be found at: [Sandbox](https://docs.connect.aptoide.com/docs/ios-sandbox-environment)

## Classes Definition and Properties

The Unity Plugin integration is based on several main classes of objects that handle its logic:

### Product

`Product` represents an in-app product.

**Properties:**

- `Sku`: String - Unique product identifier. Example: gas
- `Title`: String - The product display title. Example: Best Gas
- `Description`: String - The product description. Example: Buy gas to fill the tank.
- `PriceCurrency`: String - The user's geolocalized currency. Example: EUR
- `PriceValue`: String - The value of the product in the specified currency. Example: 0.93
- `PriceLabel`: String - The label of the price displayed to the user. Example: €0.93
- `PriceSymbol`: String - The symbol of the geolocalized currency. Example: €

### Purchase

`Purchase` represents an in-app purchase.

**Properties:**

- `UID`: String - Unique purchase identifier. Example: catappult.inapp.purchase.ABCDEFGHIJ1234
- `Sku`: String - Unique identifier for the product that was purchased. Example: gas
- `State`: String - The purchase state can be one of three: PENDING, ACKNOWLEDGED, and CONSUMED. Pending purchases are purchases that have neither been verified by the SDK nor have been consumed by the application. Acknowledged purchases are purchases that have been verified by the SDK but have not been consumed yet. Example: CONSUMED
- `OrderUID`: String - The orderUid associated with the purchase. Example: ZWYXGYZCPWHZDZUK4H
- `Payload`: String - The developer Payload. Example: 707048467.998992
- `Created`: String - The creation date for the purchase. Example: 2023-01-01T10:21:29.014456Z
- `Verification`: PurchaseVerification - The verification data associated with the purchase.

#### PurchaseVerification

`PurchaseVerification` represents an in-app purchase verification data.

**Properties:**

- `Type`: String - The type of verification made. Example: GOOGLE
- `Signature`: String - The purchase signature. Example: C4x6cr0HJk0KkRqJXUrRAhdANespHEsyx6ajRjbG5G/v3uBzlthkUe8BO7NXH/1Yi/UhS5sk7huA+hB8EbaQK9bwaiV/Z3dISl5jgYqzSEz1c/PFPwVEHZTMrdU07i/q4FD33x0LZIxrv2XYbAcyNVRY3GLJpgzAB8NvKtumbWrbV6XG4gBmYl9w4oUgJLnedii02beKlvmR7suQcqIqlSKA9WEH2s7sCxB5+kYwjQ5oHttmOQENnJXlFRBQrhW89bl18rccF05ur71wNOU6KgMcwppUccvIfXUpDFKhXQs4Ut6c492/GX1+KzbhotDmxSLQb6aw6/l/kzaSxNyjHg==
- `Data`: PurchaseVerificationData - The data associated with the verification of the purchase.

#### PurchaseVerificationData

`PurchaseVerificationData` represents the body of an in-app purchase verification data.

**Properties:**

- `OrderId`: String - The orderUid associated with the purchase. Example: 372EXWQFTVMKS6HI
- `PackageName`: String - Bundle ID of the product's application. Example: com.appcoins.trivialdrivesample
- `ProductId`: String - Unique identifier for the product that was purchased. Example: gas
- `PurchaseTime`: Integer - The time the product was purchased. Example: 1583058465823
- `PurchaseToken`: String - The token provided to the user's device when the product was purchased. Example: catappult.inapp.purchase.SZYJ5ZRWUATW5YU2
- `PurchaseState`: Integer - The purchase state of the order. Possible values are: 0 (Purchased) and 1 (Canceled)
- `DeveloperPayload`: String - A developer-specified string that contains supplemental information about an order. Example: myOrderId:12345678

### PurchaseIntent

`PurchaseIntent` represents an indirect in-app purchase intent.

**Properties:**

- `ID`: String - Unique identifier for the intent. Example: 550e8400-e29b-41d4-a716-446655440000
- `Product`: Product - The product the user wants to purchase
- `Timestamp`: String - When the intent was created. Example: 2025-01-15T10:21:29.014456Z

### AppCoinsSDKPurchaseResult

`AppCoinsSDKPurchaseResult` represents the result of a purchase operation.

**Properties:**

- `State`: String - The purchase state. Can be:
  - `AppCoinsSDK.PURCHASE_STATE_SUCCESS` - Purchase completed successfully
  - `AppCoinsSDK.PURCHASE_STATE_PENDING` - Purchase is pending
  - `AppCoinsSDK.PURCHASE_STATE_USER_CANCELLED` - User cancelled the purchase
  - `AppCoinsSDK.PURCHASE_STATE_FAILED` - Purchase failed
- `Value`: Object (only present when State is SUCCESS) containing:
  - `VerificationResult`: String - Can be `AppCoinsSDK.PURCHASE_VERIFICATION_STATE_VERIFIED` or `AppCoinsSDK.PURCHASE_VERIFICATION_STATE_UNVERIFIED`
  - `Purchase`: Purchase - The purchase object
  - `VerificationError`: AppCoinsSDKError (optional) - Error details if verification failed
- `Error`: AppCoinsSDKError (only present when State is FAILED) - Error details

### AppCoinsSDKResult&lt;T&gt;

`AppCoinsSDKResult<T>` represents the result of SDK operations that return data.

**Properties:**

- `IsSuccess`: Boolean - Whether the operation succeeded
- `Value`: T - The result value (only present when IsSuccess is true)
- `Error`: AppCoinsSDKError (only present when IsSuccess is false) - Error details

**Used by:**
- `GetProducts()` - Returns `AppCoinsSDKResult<Product[]>`
- `GetAllPurchases()` - Returns `AppCoinsSDKResult<Purchase[]>`
- `GetLatestPurchase(sku)` - Returns `AppCoinsSDKResult<Purchase>`
- `GetUnfinishedPurchases()` - Returns `AppCoinsSDKResult<Purchase[]>`
- `ConsumePurchase(sku)` - Returns `AppCoinsSDKResult<bool>`
- `GetTestingWalletAddress()` - Returns `AppCoinsSDKResult<string>`
- `GetPurchaseIntent()` - Returns `AppCoinsSDKResult<PurchaseIntent>`

### AppCoinsSDKError

`AppCoinsSDKError` represents error information when an SDK operation fails.

**Properties:**

- `Type`: String - The error type. Can be:
  - `networkError` - Network-related error
  - `systemError` - System or SDK error
  - `notEntitled` - User is not entitled to the product
  - `productUnavailable` - Product is not available
  - `purchaseNotAllowed` - Purchase is not allowed
  - `unknown` - Unknown error
- `Message`: String - A brief error message
- `Description`: String - A detailed error description
- `Request`: ErrorRequest (optional) - Request details if available

#### ErrorRequest

**Properties:**

- `URL`: String - The request URL
- `Method`: String - The HTTP method
- `Body`: String - The request body
- `ResponseData`: String - The response data
- `StatusCode`: Integer - The HTTP status code

### AppCoinsSDK

This class is responsible for general purpose methods and provides singleton access via `AppCoinsSDK.Instance`.
