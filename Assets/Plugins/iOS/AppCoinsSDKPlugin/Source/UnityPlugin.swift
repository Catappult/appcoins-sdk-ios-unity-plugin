import Foundation
import AppCoinsSDK

public struct ProductData {
    public let sku: String
    public let title: String
    public let description: String
    public let priceCurrency: String
    public let priceValue: String
    public let priceLabel: String
    public let priceSymbol: String
}

extension ProductData {
    var dictionaryRepresentation: [String: Any] {
        var dict = [String: Any]()
        dict["sku"] = sku
        dict["title"] = title
        dict["description"] = description
        dict["priceCurrency"] = priceCurrency
        dict["priceValue"] = priceValue
        dict["priceLabel"] = priceLabel
        dict["priceSymbol"] = priceSymbol
        return dict
    }
}

@objc public class UnityPlugin : NSObject {
    
    @objc public static let shared = UnityPlugin()

    @objc public func isAvailable(completion: @escaping ([String: Any]) -> Void) {
        Task {
            let sdkAvailable = await AppcSDK.isAvailable()
            let dictionaryRepresentation = ["Available": sdkAvailable]
            completion(dictionaryRepresentation)
        }
    }

    @objc public func getProducts(skus: [String], completion: @escaping ([[String: Any]]) -> Void) {
        Task {
            var products = [Product]()
            var productItems = [ProductData]()
            
            do {
                if (skus.isEmpty) {
                    products = try await Product.products()
                } else {
                    products = try await Product.products(for: skus)
                }
                
                productItems = products.map { product in
                                        ProductData(
                                            sku: product.sku,
                                            title: product.title,
                                            description: product.description ?? "",
                                            priceCurrency: product.priceCurrency,
                                            priceValue: product.priceValue,
                                            priceLabel: product.priceLabel,
                                            priceSymbol: product.priceSymbol
                                        )
                                    }
            } catch {
                print("Error")
            }
            
            let arrayOfDictionaries = productItems.map { $0.dictionaryRepresentation }
            completion(arrayOfDictionaries)
        }
    }

    @objc public func purchase(sku: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            let products = try await Product.products(for: [sku])
            let result = await products.first?.purchase()
            
            var state = ""
            var errorMessage = ""
            
            switch result {
                case .success(let verificationResult):
                     switch verificationResult {
                           case .verified(let purchase):
                                state = "success"
                                try await purchase.finish()
                           case .unverified(let purchase, let verificationError):
                                state = "unverified"
                                errorMessage = verificationError.localizedDescription
                                
                     }
                case .pending:
                    state = "pending"
                case .userCancelled:
                    state = "userCancelled"
                case .failed(let error):
                    state = "failed"
                    errorMessage = error.localizedDescription
            case .none:
                state = "none"
            }
            
            let dictionaryRepresentation = ["state": state, "error": errorMessage ]
            completion(dictionaryRepresentation)
        }
    }
}
