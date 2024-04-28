# AppCoins SDK for Unity / iOS

The AppCoins SDK for Unity / iOS is a wrapper over [AppCoins SDK for iOS](https://github.com/Catappult/appcoins-sdk-ios) 

## Tested on

- Unity 2022.3.22f1.
- iOS 17.4.

## Download

Please import the plugin using latest version from the [GitHub releases page](https://github.com/Catappult/appcoins-sdk-ios-unity-plugin/releases).

### Implementation

Now that you have the SDK and necessary permissions set-up you can start making use of its functionalities. To do so you must import the SDK module in any files you want to use it by calling the following: `import AppCoinsSDK`.

1. **Check AppCoins SDK Availability**  
   The AppCoins SDK will only be available on devices in the European Union with an iOS version equal to or higher than 17.4. Therefore, before attempting any purchase, you should check if the SDK is available by calling `AppCoinsSDK.Instance.IsAvailable()`.

   ```c#
   var sdkAvailable = await AppCoinsSDK.Instance.IsAvailable();
   
   if (sdkAvailable)
   {
   	// make purchase
   }
   ```

2. **Query In-App Products**  
   You should start by getting the In-App Products you want to make available to the user. You can do this by calling `Product.products`.

   This method can either return all of your Catappult In-App Products or a specific list.

   1. `AppCoinsSDK.Instance.GetProducts()`

      Returns all application Catappult In-App Products:

      ```c#
      var products = await AppCoinsSDK.Instance.GetProducts();
      ```
   2. `AppCoinsSDK.Instance.GetProducts(skus)`

      Returns a specific list of Catappult In-App Products:

      ```swift
      var selectedProducts = await AppCoinsSDK.Instance.GetProducts(new string[] { "coins_100", "gas" });
      ```

3. **Purchase In-App Product**  
   To purchase an In-App Product you must call the function `AppCoinsSDK.Instance.Purchase(sku, payload)`. The SDK will handle all of the purchase logic for you and it will return you on completion the result of the purchase. This result is an object with the following properties:

   - Success: bool

   - State: string (`AppCoinsSDK.PURCHASE_STATE_SUCCESS`, `AppCoinsSDK.PURCHASE_STATE_UNVERIFIED`, `AppCoinsSDK.PURCHASE_STATE_USER_CANCELLED`, `AppCoinsSDK.PURCHASE_STATE_FAILED`)

   - Error: string


   In case of success the application will verify the transaction’s signature locally. After this verification you should handle its result:  
          – If the purchase is verified you should consume the item and give it to the user:  
          – If it is not verified you need to make a decision based on your business logic, you either still consume the item and give it to the user, or otherwise the purchase will not be acknowledged and we will refund the user in 24 hours.

   In case of failure you can deal with different types of error in a switch statement. 

   You can also pass a payload to the purchase method in order to associate some sort of information with a specific purchase. <br/>

   ```c#
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

4. **Query Purchases**  
   You can query the user’s purchases by using one of the following methods:

   1. `AppCoinsSDK.Instance.GetAllPurchases()`

      This method returns all purchases that the user has performed in your application.

      ```c#
      var purchases = await AppCoinsSDK.Instance.GetAllPurchases();
      ```
   2. `AppCoinsSDK.Instance.GetLatestPurchase(string sku)`

      This method returns the latest user purchase for a specific In-App Product.

      ```c#
      var latestPurchase = await AppCoinsSDK.Instance.GetLatestPurchase("gas");
      ```
   3. `AppCoinsSDK.Instance.GetUnfinishedPurchases()`

      This method returns all of the user’s unfinished purchases in the application. An unfinished purchase is any purchase that has neither been acknowledged (verified by the SDK) nor consumed. You can use this method for consuming any unfinished purchases.

      ```c#
      var unfinishedPurchases = await AppCoinsSDK.Instance.GetUnfinishedPurchases();
      ```

## Classes Definition and Properties

The SDK integration is based on four main classes of objects that handle its logic:

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

### Purchase

`PurchaseData` represents an in-app purchase.

**Properties:**

- `UID`: String - Unique purchase identifier. Example: catappult.inapp.purchase.ABCDEFGHIJ1234
- `Sku`: String - Unique identifier for the product that was purchased. Example: gas
- `State`: String - The purchase state can be one of three: PENDING, ACKNOWLEDGED, and CONSUMED. Pending purchases are purchases that have neither been verified by the SDK nor have been consumed by the application. Acknowledged purchases are purchases that have been verified by the SDK but have not been consumed yet. Example: CONSUMED
- `OrderUID`: String - The orderUid associated with the purchase. Example: ZWYXGYZCPWHZDZUK4H
- `Payload`: String - The developer Payload. Example: 707048467.998992
- `Created`: String - The creation date for the purchase. Example: 2023-01-01T10:21:29.014456Z

### AppCoinsSDK

This class is responsible for general purpose methods.
