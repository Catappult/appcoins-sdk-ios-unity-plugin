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