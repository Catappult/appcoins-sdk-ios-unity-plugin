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
        dict["Sku"] = sku
        dict["Title"] = title
        dict["Description"] = description
        dict["PriceCurrency"] = priceCurrency
        dict["PriceValue"] = priceValue
        dict["PriceLabel"] = priceLabel
        dict["PriceSymbol"] = priceSymbol
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
        dict["UID"] = uid
        dict["Sku"] = sku
        dict["State"] = state
        dict["OrderUID"] = orderUid
        dict["Payload"] = payload
        dict["Created"] = created
        return dict
    }
}

@objc public class UnityPlugin : NSObject {
    
    @objc public static let shared = UnityPlugin()

    @objc public func handleDeepLink(url: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            if let urlObject = URL(string: url) {
                if AppcSDK.handle(redirectURL: urlObject) {
                    completion(["Success": true])
                }
                else {
                    completion(["Success": false])
                }
            } else {
                // Handle the case where the URL conversion fails
                completion(["Success": false, "Error": "Invalid URL"])
            }
        }
    }

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
                print("Error: \(error)")
            }
            
            let arrayOfDictionaries = productItems.map { $0.dictionaryRepresentation }
            completion(arrayOfDictionaries)
        }
    }

    @objc public func purchase(sku: String, payload: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            let products = try await Product.products(for: [sku])

            let payloadToSend = payload.isEmpty ? nil : payload
            let result = await products.first?.purchase(payload: payloadToSend)
            
            var state = ""
            var errorMessage = ""
            var purchaseSku = ""
            var payload = ""
            
            switch result {
                case .success(let verificationResult):
                     switch verificationResult {
                           case .verified(let purchase):
                                state = "success"
                                purchaseSku = purchase.sku
                                payload = purchase.payload ?? ""
                           case .unverified(let purchase, let verificationError):
                                state = "unverified"
                                errorMessage = verificationError.localizedDescription
                                purchaseSku = purchase.sku
                                
                     }
                case .pending:
                    state = "pending"
                case .userCancelled:
                    state = "user_cancelled"
                case .failed(let error):
                    state = "failed"
                    errorMessage = error.localizedDescription
                    print("errorMessage: \(errorMessage)")
            case .none:
                state = "none"
            }
            
            let dictionaryRepresentation = ["State": state, "Error": errorMessage, "PurchaseSku": purchaseSku, "Payload": payload ]
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
                    let purchase = try await Purchase.latest(sku: sku)
                    let purchaseItem = PurchaseData(
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

    @objc public func consumePurchase(sku: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            do {
                let purchases = try await Purchase.all()
                
                if let purchaseToConsume = purchases.first(where: { $0.sku == sku && $0.state == "ACKNOWLEDGED" }) {
                    try await purchaseToConsume.finish()
                    let dictionaryRepresentation = ["Success": true, "Error": ""]
                    completion(dictionaryRepresentation)
                } else {
                    let dictionaryRepresentation = ["Success": false, "Error": "No purchase to consume found for the given SKU."]
                    completion(dictionaryRepresentation)
                }
            } catch {
                let dictionaryRepresentation = ["Success": false, "Error": "An error occurred: \(error.localizedDescription)"]
                completion(dictionaryRepresentation)
            }
        }
    }
}
