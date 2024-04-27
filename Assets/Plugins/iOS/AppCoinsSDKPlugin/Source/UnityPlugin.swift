
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
    @objc public func initialize(completion: @escaping ([[String: Any]]) -> Void) {
        Task {
            var productItems = [ProductData]()

            print("Checking SDK...")
            if await AppcSDK.isAvailable() {
                print("SDK is available!")
                
                let address = Sandbox.getTestingWalletAddress()
                print("Testing Wallet Address: " + (address ?? ""))
                
                do {
                    print("Getting products...")
                    let products = try await Product.products()
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
                    print("Found \(products.count) products")
                } catch {
                    print("Error")
                }
            } else {
                print("SDK is not available!!!")
                productItems = []
            }
            
            let arrayOfDictionaries = productItems.map { $0.dictionaryRepresentation }

            completion(arrayOfDictionaries)
        }
    }
    
    @objc public func purchase(withSKU sku: String, completion: @escaping (String) -> Void) {
        Task {
            do {
                print("Fetching product...")
                let products = try await Product.products(for: [sku])
                if let product = products.first {
                    print("Product found, attempting purchase...")
                    let result = await product.purchase()

                    switch result {
                    case .success(let verificationResult):
                        switch verificationResult {
                        case .verified(let purchase):
                            try await purchase.finish()
                            let dictionary = PurchaseData(
                                uid: purchase.uid,
                                sku: purchase.sku,
                                state: "verified",
                                orderUid: purchase.orderUid,
                                payload: purchase.payload,
                                created: purchase.created
                            ).dictionaryRepresentation
                            let jsonData = try JSONSerialization.data(withJSONObject: dictionary, options: [])
                            let jsonString = String(data: jsonData, encoding: .utf8) ?? "{}"
                            completion(jsonString)

                        case .unverified(let purchase, let verificationError):
                            print("Purchase unverified: \(verificationError)")
                            completion("{\"error\": \"Purchase unverified: \(verificationError.localizedDescription)\"}")

                        }
                    case .pending:
                        print("Purchase pending")
                        completion("{\"state\": \"pending\"}")

                    case .userCancelled:
                        print("Purchase cancelled by user")
                        completion("{\"error\": \"Purchase cancelled by user\"}")

                    case .failed(let error):
                        let errorMessage: String
                        switch error {
                        case .networkError:
                            errorMessage = "Network related errors"
                        case .systemError:
                            errorMessage = "Internal APPC system errors"
                        case .notEntitled:
                            errorMessage = "The host app does not have proper entitlements configured"
                        case .productUnavailable:
                            errorMessage = "The product is not available"
                        case .purchaseNotAllowed:
                            errorMessage = "The user was not allowed to perform the purchase"
                        case .unknown:
                            errorMessage = "Other error"
                        }
                        
                        print("Purchase failed: \(errorMessage)")
                        completion("{\"error\": \"Purchase failed: \(errorMessage)\"}")
                    }
                } else {
                    print("Product not found for SKU: \(sku)")
                    completion("{\"error\": \"Product not found\"}")
                }
            } catch {
                print("Error during purchase process: \(error)")
                completion("{\"error\": \"\(error.localizedDescription)\"}")
            }
        }
    }
    
    @objc public func listPurchases(completion: @escaping ([[String: Any]]) -> Void) {
        Task {
            var purchaseItems = [PurchaseData]()

            do {
                print("Getting list purchases...")
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
                print("Found \(purchases.count) purchases")
            } catch {
                print("Error")
            }
            
            let arrayOfDictionaries = purchaseItems.map { $0.dictionaryRepresentation }

            completion(arrayOfDictionaries)
        }
    }

}

