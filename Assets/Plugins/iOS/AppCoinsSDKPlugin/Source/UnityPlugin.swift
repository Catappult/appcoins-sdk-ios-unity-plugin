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

public struct PurchaseData {
    public let uid: String
    public let sku: String
    public var state: String
    public let orderUid: String
    public let payload: String?
    public let created: String
}

extension PurchaseData {
    var dictionaryRepresentation: [String: Any] {
        var dict = [String: Any]()
        dict["uid"] = uid
        dict["sku"] = sku
        dict["state"] = state
        dict["orderUid"] = orderUid
        dict["payload"] = payload
        dict["created"] = created
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

    @objc public func getAllPurchases(completion: @escaping ([[String: Any]]) -> Void) {
        Task {
            var purchaseItems = [PurchaseData]()
            
            do {
                    let purchases = try await Purchase.all()
                    purchaseItems = purchases.map { purchase in
                        PurchaseData(
                            uid: purchase.uid,
                            sku: purchase.sku,
                            state: purchase.state,
                            orderUid: purchase.orderUid,
                            payload: purchase.payload,
                            created: purchase.created
                        )
                    }
            } catch {
                print("Error")
            }
            
            let arrayOfDictionaries = purchaseItems.map { $0.dictionaryRepresentation }
            completion(arrayOfDictionaries)
        }
    }

    @objc public func getLatestPurchase(sku: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            do {
                    var purchase = try await Purchase.latest(sku: sku)
                    var purchaseItem = PurchaseData(
                        uid: purchase?.uid ?? "",
                            sku: purchase?.sku ?? "",
                            state: purchase?.state ?? "",
                            orderUid: purchase?.orderUid ?? "",
                            payload: purchase?.payload ?? "",
                            created: purchase?.created ?? ""
                        )
                completion(purchaseItem.dictionaryRepresentation)
            } catch {
                print("Error")
            }
        }
    }

    @objc public func getUnfinishedPurchases(completion: @escaping ([[String: Any]]) -> Void) {
        Task {
            var purchases = [Purchase]()
            var purchaseItems = [PurchaseData]()
            
            do {
                    let purchases = try await Purchase.unfinished()
                    purchaseItems = purchases.map { purchase in
                        PurchaseData(
                            uid: purchase.uid,
                            sku: purchase.sku,
                            state: purchase.state,
                            orderUid: purchase.orderUid,
                            payload: purchase.payload,
                            created: purchase.created
                        )
                    }
            } catch {
                print("Error")
            }
            
            let arrayOfDictionaries = purchaseItems.map { $0.dictionaryRepresentation }
            completion(arrayOfDictionaries)
        }
    }
}
