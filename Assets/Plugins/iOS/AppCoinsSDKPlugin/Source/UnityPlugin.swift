import Foundation
import AppCoinsSDK
import UIKit

public struct ProductData {
    public let sku: String
    public let title: String
    public let description: String
    public let priceCurrency: String
    public let priceValue: String
    public let priceLabel: String
    public let priceSymbol: String
    
    init(product: Product) {
        self.sku = product.sku
        self.title = product.title
        self.description = product.description ?? ""
        self.priceCurrency = product.priceCurrency
        self.priceValue = product.priceValue
        self.priceLabel = product.priceLabel
        self.priceSymbol = product.priceSymbol
    }
    
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
    public let verification: PurchaseVerification
    
    init(purchase: Purchase) {
        self.uid = purchase.uid
        self.sku = purchase.sku
        self.state = purchase.state
        self.orderUid = purchase.orderUid
        self.payload = purchase.payload
        self.created = purchase.created
        self.verification = PurchaseVerification(verification: purchase.verification)
            
    }

    public struct PurchaseVerification {
        public let type: String
        public let data: PurchaseVerificationData
        public let signature: String
        
        init(verification: Purchase.PurchaseVerification) {
            self.type = verification.type
            self.data = PurchaseVerificationData(verificationData: verification.data)
            self.signature = verification.signature
        }
    }
    
    public struct PurchaseVerificationData: Codable {
        public let orderId: String
        public let packageName: String
        public let productId: String
        public let purchaseTime: Int
        public let purchaseToken: String
        public let purchaseState: Int
        public let developerPayload: String
        
        init(verificationData: Purchase.PurchaseVerificationData) {
            self.orderId = verificationData.orderId
            self.packageName = verificationData.packageName
            self.productId = verificationData.productId
            self.purchaseTime = verificationData.purchaseTime
            self.purchaseToken = verificationData.purchaseToken
            self.purchaseState = verificationData.purchaseState
            self.developerPayload = verificationData.developerPayload
        }
    }
    
    var dictionaryRepresentation: [String: Any] {
        var purchaseDictionary = [String: Any]()
        purchaseDictionary["UID"] = uid
        purchaseDictionary["Sku"] = sku
        purchaseDictionary["State"] = state
        purchaseDictionary["OrderUID"] = orderUid
        purchaseDictionary["Payload"] = payload
        purchaseDictionary["Created"] = created
        
        var verificationDictionary = [String: Any]()
        verificationDictionary["Type"] = verification.type
        verificationDictionary["Signature"] = verification.signature
        
        var verificationDataDictionary = [String: Any]()
        verificationDataDictionary["OrderId"] = verification.data.orderId
        verificationDataDictionary["PackageName"] = verification.data.packageName
        verificationDataDictionary["ProductId"] = verification.data.productId
        verificationDataDictionary["PurchaseTime"] = verification.data.purchaseTime
        verificationDataDictionary["PurchaseToken"] = verification.data.purchaseToken
        verificationDataDictionary["PurchaseState"] = verification.data.purchaseState
        verificationDataDictionary["DeveloperPayload"] = verification.data.developerPayload
        
        verificationDictionary["Data"] = verificationDataDictionary
        purchaseDictionary["Verification"] = verificationDictionary
        
        return purchaseDictionary
    }
}


@objcMembers
@objc public class UnityPlugin : NSObject {
    
    @objc public static let shared = UnityPlugin()

    @objc public func handleDeepLink(url: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            guard let urlObject = URL(string: url) else {
                return completion(["Success": false, "Error": "Invalid URL"])
            }
            completion(["Success": AppcSDK.handle(redirectURL: urlObject)])
        }
    }

    @objc public func isAvailable(completion: @escaping ([String: Any]) -> Void) {
        Task {
            completion(["Available": await AppcSDK.isAvailable()])
        }
    }

    @objc public func getProducts(skus: [String], completion: @escaping ([[String: Any]]) -> Void) {
        Task {
            do {
                let products = try await (skus.isEmpty ? Product.products() : Product.products(for: skus))
                completion( products.map { ProductData(product: $0).dictionaryRepresentation } )
            } catch {
                completion([])
            }
        }
    }

    @objc public func purchase(sku: String, payload: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            do {
                guard let product = try await Product.products(for: [sku]).first else {
                    return completion(["State": "failed", "Error": "Product not found", "Purchase": ""])
                }

                let result = await product.purchase(payload: payload.isEmpty ? nil : payload)
                
                let response: [String: Any] = {
                    switch result {
                    case .success(let verificationResult):
                        switch verificationResult {
                        case .verified(let purchase):
                            return ["State": "success", "Error": "", "Purchase": PurchaseData(purchase: purchase).dictionaryRepresentation]
                        case .unverified(let purchase, let verificationError):
                            return ["State": "unverified", "Error": verificationError.localizedDescription, "Purchase": PurchaseData(purchase: purchase).dictionaryRepresentation]
                        }
                    case .pending:
                        return ["State": "pending", "Error": "", "Purchase": ""]
                    case .userCancelled:
                        return ["State": "user_cancelled", "Error": "", "Purchase": ""]
                    case .failed(let error):
                        return ["State": "failed", "Error": error.localizedDescription, "Purchase": ""]
                    }
                }()
                
                completion(response)
            } catch {
                completion(["State": "failed", "Error": error.localizedDescription, "Purchase": ""])
            }
        }
    }

    @objc public func getAllPurchases(completion: @escaping ([[String: Any]]) -> Void) {
        Task {
            do {
                let purchases = try await Purchase.all()
                completion(purchases.map { PurchaseData(purchase: $0).dictionaryRepresentation })
            } catch {
                completion([])
            }
        }
    }

    @objc public func getLatestPurchase(sku: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            guard let purchase = try? await Purchase.latest(sku: sku) else {
                completion([:])
                return
            }
        
            completion(PurchaseData(purchase: purchase).dictionaryRepresentation)
        }
    }

    @objc public func getUnfinishedPurchases(completion: @escaping ([[String: Any]]) -> Void) {
        Task {
            do {
                let purchases = try await Purchase.unfinished()
                completion(purchases.map { PurchaseData(purchase: $0).dictionaryRepresentation })
            } catch {
                completion([])
            }
        }
    }

    @objc public func consumePurchase(sku: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            do {
                guard let purchase = try await Purchase.all().first(where: { $0.sku == sku && $0.state == "ACKNOWLEDGED" }) else {
                    return completion(["Success": false, "Error": "No purchase to consume found for the given SKU."])
                }
                
                try await purchase.finish()
                completion(["Success": true, "Error": ""])
            } catch {
                completion(["Success": false, "Error": "An error occurred: \(error.localizedDescription)"])
            }
        }
    }
    
    @objc public func getTestingWalletAddress() -> String? {
        return Sandbox.getTestingWalletAddress()
    }
    
    // The Task that listens for Purchase.updates
    private var task: Task<Void, Never>?
    // Flag to track if we're already observing
    private var isObserving = false

    /// Start observing purchases and send them to Unity via UnitySendMessage
    @objc public func startObservingPurchases() {
        guard !isObserving else { return }
        isObserving = true

        task = Task(priority: .background) {
            // Because Purchase.updates yields VerificationResult<Purchase> in your snippet
            for await verificationResult in Purchase.updates {
                
                // 1) Build a dictionary describing the verification result
                let response: [String: Any] = {
                    switch verificationResult {
                    case .verified(let purchase):
                        return [
                            "State": "success",
                            "Error": "",
                            "Purchase": PurchaseData(purchase: purchase).dictionaryRepresentation
                        ]
                    case .unverified(let purchase, let verificationError):
                        return [
                            "State": "unverified",
                            "Error": verificationError.localizedDescription,
                            "Purchase": PurchaseData(purchase: purchase).dictionaryRepresentation
                        ]
                    }
                }()
                
                // 2) Serialize that dictionary to JSON
                guard let cString = dictionaryToCString(response) else {
                    continue // if serialization fails, skip
                }

                // 3) Send to Unity (the third parameter must be a C-string)
                UnitySendMessageBridge("AppCoinsPurchaseManager", "OnPurchaseUpdated", cString)
            }
        }
    }
    
    private func dictionaryToCString(_ dict: [String: Any]) -> UnsafeMutablePointer<Int8>? {
        do {
            let data = try JSONSerialization.data(withJSONObject: dict, options: [])
            if let jsonString = String(data: data, encoding: .utf8) {
                return strdup(jsonString) // Allocate C string from Swift string
            }
        } catch {
            print("Failed to serialize dictionary: \(error)")
        }
        return strdup("{}") // Return "{}" if serialization fails
    }
}

// Declare the bridge function to make it accessible in Swift
@_silgen_name("UnitySendMessageBridge")
func UnitySendMessageBridge(_ objectName: UnsafePointer<Int8>, _ methodName: UnsafePointer<Int8>, _ message: UnsafePointer<Int8>)
