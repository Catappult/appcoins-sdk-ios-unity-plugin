# AppCoinsSDK for Unity on iOS

## Introduction

AppCoinsSDK is a Unity plugin designed for iOS that simplifies in-app purchasing and product management within games. This wrapper allows Unity developers to integrate native iOS in-app purchase features into their games using a straightforward C# interface.

## Features

- Initialize in-app purchases with a single call.
- Fetch and display product information.
- Handle purchases and manage purchase states.
- Simulate purchases when running in non-iOS environments.

## Requirements

- Unity 2019.4 or later.
- iOS 12.0 or higher.

## Installation

1. Clone this repository or download the latest release.
2. Import `AppCoinsSDK.unitypackage` into your Unity project.
3. Ensure your project is set to build for iOS in Unity’s Build Settings.

## Usage

### Initializing the SDK

Call `AppCoinsSDK.Instance.Initialize()` at the start of your application. It will handle all setup required for in-app purchases and product fetching.

```
csharpCopy code
AppCoinsSDK.Instance.Initialize();
```

### Fetching Products

To fetch products, listen to the `OnProductsReceived` event after initialization:

```
csharpCopy code
AppCoinsSDK.Instance.OnProductsReceived += HandleProductsReceived;

void HandleProductsReceived(object sender, ProductsReceivedEventArgs e)
{
    if (e.Available)
    {
        foreach (var product in e.Products)
        {
            Debug.Log($"{product.title} - {product.priceLabel}");
        }
    }
}
```

### Making a Purchase

To make a purchase, call the `Purchase` method with the SKU of the product:

```
csharpCopy code
AppCoinsSDK.Instance.Purchase("gem_pack_100");
```

### Handling Purchases

Implement callback methods to handle the purchase results:

```
csharpCopy code
void OnPurchaseComplete(string result)
{
    var response = JsonUtility.FromJson<PurchaseResponse>(result);
    if (response.state == "verified")
    {
        Debug.Log("Purchase successful!");
        // Grant the purchased item to the user
    }
}
```

## Example Project

An example project is included in the `/Examples` directory that demonstrates how to use the AppCoinsSDK.

## Contributing

Contributions are welcome! Please fork the repository and submit pull requests with your features and bug fixes.

## License

This project is licensed under the MIT License - see the [LICENSE.md](https://chat.openai.com/c/LICENSE.md) file for details.

## Support

If you encounter any issues or have questions, please file an issue on the GitHub repository.
=======
# AppCoins SDK for Unity / iOS

The AppCoins SDK for Unity / iOS is a wrapper over [AppCoins SDK for iOS](https://github.com/Catappult/appcoins-sdk-ios) 

## Tested on

- Unity 2022.3.26f1.
- iOS 17.4.

## Download

Please import the plugin using latest version from the [GitHub releases page](https://github.com/Catappult/appcoins-sdk-ios-unity-plugin/releases).

### Implementation

Now that you have imported the plugin you can start making use of its functionalities.

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
