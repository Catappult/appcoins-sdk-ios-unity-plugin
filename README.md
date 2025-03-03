The iOS Billing SDK is a straightforward solution for implementing Catappult billing. Its Unity Plugin provides a simple interface for Unity games to communicate with the SDK. It comprises a Billing client that integrates with AppCoins Wallet, enabling you to retrieve your products from Catappult and facilitate the purchase of those items.

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
   The AppCoins Billing will only be available on devices in the European Union with an iOS version equal to or higher than 17.4 and only in applications distributed through the Aptoide iOS App Store. Therefore, before attempting any purchase, you should check if the SDK is available by calling `AppCoinsSDK.Instance.IsAvailable()`.

   ```csharp
   var isAvailable = await AppCoinsSDK.Instance.IsAvailable();

   if (isAvailable) 
   {
     // make purchase
   }
   ```
2. **Query In-App Products**  
   You should start by getting the In-App Products you want to make available to the user. You can do this by calling `Product.products`.  
   This method can either return all of your Catappult In-App Products or a specific list.

   1. `AppCoinsSDK.Instance.GetProducts()`

      Returns all application Catappult In-App Products:

      ```csharp
      var products = await AppCoinsSDK.Instance.GetProducts();
      ```
   2. `AppCoinsSDK.Instance.GetProducts(skus)`

      Returns a specific list of Catappult In-App Products:

      ```csharp
      var products = await AppCoinsSDK.Instance.GetProducts(new string[] { "coins_100", "gas" });
      ```

   > ⚠️ **Warning:** You will only be able to query your In-App Products once your application is reviewed and approved on Aptoide Connect.
   
3. **Purchase In-App Product**  
   To purchase an In-App Product you must call the function `AppCoinsSDK.Instance.Purchase(sku, payload)`. The Plugin will handle all of the purchase logic for you and it will return you on completion the result of the purchase. This result is an object with the following properties:        

   1. State: `string` (`AppCoinsSDK.PURCHASE_STATE_SUCCESS`, `AppCoinsSDK.PURCHASE_STATE_UNVERIFIED`, `AppCoinsSDK.PURCHASE_STATE_USER_CANCELLED`, `AppCoinsSDK.PURCHASE_STATE_FAILED`)
   2. Error: `string`
   3. Purchase: `PurchaseData`

   In case of success the application will verify the transaction’s signature locally. After this verification you should handle its result:

   1. If the purchase is verified you should consume the item and give it to the user.
   2. If it is not verified you need to make a decision based on your business logic, you either still consume the item and give it to the user, or otherwise the purchase will not be acknowledged and we will refund the user in 24 hours.

   In case of failure you can deal with different types of errors.

   You can also pass a Payload to the purchase method in order to associate some sort of information with a specific purchase. You can use this for example to associate a specific user with a Purchase: `gas.purchase(payload: "User123")`.  
   <br/>

   ```csharp
   var purchaseResponse = await AppCoinsSDK.Instance.Purchase("gas", "User123");

   if (purchaseResponse.State == AppCoinsSDK.PURCHASE_STATE_SUCCESS)
   {
     var response = await AppCoinsSDK.Instance.ConsumePurchase(purchaseResponse.PurchaseSku);

     if (response.Success)
     {
         Debug.Log("Purchase consumed successfully");
     }
     else
     {
         Debug.Log("Error consuming purchase: " + response.Error);
     }
   }
   ```
4. **Handle Indirect Purchases**

   In addition to standard In-App Purchases, the AppCoins SDK supports **Indirect In-App Purchases**. These are purchases that users do not initiate directly through a Buy action within the application. Common use cases include:

   1. Purchasing an item directly from a catalog of In-App Products in the Aptoide Store.
   2. Buying an item through a web link.

   The `AppCoinsPurchaseManager.OnPurchaseUpdated` Unity Action allows developers to manage these purchases and assign consumables to users. This action continuously streams purchase updates, ensuring real-time transaction synchronization.

   The event returns a `PurchaseResponse` object, which should be handled in the same way as a standard In-App Purchase.

   To properly handle purchase updates, define the subscription within a singleton class, ensuring it remains active for the application’s lifecycle. Use the same HandlePurchase method applied to standard purchases.
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
     
     // Subscribe to purchase updates
     AppCoinsPurchaseManager.OnPurchaseUpdated += HandlePurchase;
   }
   
   // Already defined previously
   async public void HandlePurchase(PurchaseResponse purchaseResponse)
   {
     if (purchaseResponse.State == AppCoinsSDK.PURCHASE_STATE_SUCCESS)
     {
       var response = await AppCoinsSDK.Instance.ConsumePurchase(purchaseResponse.PurchaseSku);
       
       if (response.Success)
       {
         Debug.Log("Purchase consumed successfully");
       }
       else
       {
         Debug.Log("Error consuming purchase: " + response.Error);
       }
     }
   }
   ```

5. **Query Purchases**  
   You can query the user’s purchases by using one of the following methods:

   1. `AppCoinsSDK.Instance.GetAllPurchases()`

      This method returns all purchases that the user has performed in your application.

      ```csharp
      var purchases = await AppCoinsSDK.Instance.GetAllPurchases();
      ```
   2. `AppCoinsSDK.Instance.GetLatestPurchase(string sku)`

      This method returns the latest user purchase for a specific In-App Product.

      ```csharp
      var latestPurchase = await AppCoinsSDK.Instance.GetLatestPurchase("gas");
      ```
   3. `AppCoinsSDK.Instance.GetUnfinishedPurchases()`

      This method returns all of the user’s unfinished purchases in the application. An unfinished purchase is any purchase that has neither been acknowledged (verified by the SDK) nor consumed. You can use this method for consuming any unfinished purchases.

      ```csharp
      var unfinishedPurchases = await AppCoinsSDK.Instance.GetUnfinishedPurchases();
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

You can test the in-app purchase (IAP) functionality using Catappult’s **Sandbox environment**. Follow these steps to set it up:

1. Retrieve your testing wallet address by calling `AppCoinsSDK.Instance.GetTestingWalletAddress()`.
2. Add your testing wallet address to the **Sandbox menu** in the Developer Console, and use this wallet to make test purchases in your app.

> ⚠️ **Warning:** You must have proven ownership of the app to access the Sandbox environment and test IAPs.

> ⚠️ **Warning:** Do not delete the app from the testing device, as this will remove the testing wallet. If the app is deleted, you’ll need to obtain a new wallet address and add it again to the Sandbox.

#### Testing In-App Purchases

1. Select the item you wish to purchase in the app.
2. Choose the **Sandbox** option for the transaction.
3. Once the purchase is completed, verify the transaction in the Wallet by checking the **Sandbox transactions**.
4. Ensure the purchased item is correctly received in the app.

If all steps are successful, your billing solution is fully integrated!

For more detailed instructions, refer to [Catappult's documentation](https://docs.catappult.io/docs/ios-sandbox-environment).

## Classes Definition and Properties

The Unity Plugin integration is based on three main classes of objects that handle its logic:

### ProductData

`ProductData` represents an in-app product.

**Properties:**

- `Sku`: String - Unique product identifier. Example: gas
- `Title`: String - The product display title. Example: Best Gas
- `Description`: String? - The product description. Example: Buy gas to fill the tank.
- `PriceCurrency`: String - The user’s geolocalized currency. Example: EUR
- `PriceValue`: String - The value of the product in the specified currency. Example: 0.93
- `PriceLabel`: String - The label of the price displayed to the user. Example: €0.93
- `PriceSymbol`: String - The symbol of the geolocalized currency. Example: €

### PurchaseData

`PurchaseData` represents an in-app purchase.

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


### AppCoinsSDK

This class is responsible for general purpose methods.
