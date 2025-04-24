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

public struct PurchaseIntentData {
    public let id: String
    public let product: ProductData
    public let timestamp: String
    
    init(intent: PurchaseIntent) {
        self.id = intent.id.uuidString
        self.product = ProductData(product: intent.product)
        
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.timeZone = TimeZone(secondsFromGMT: 0)
        formatter.dateFormat = "yyyy-MM-dd'T'HH:mm:ss.SSSZZZZZ"
        self.timestamp = formatter.string(from: intent.timestamp)
    }
    
    
    var dictionaryRepresentation: [String: Any] {
        var purchaseIntentDictionary = [String: Any]()
        purchaseIntentDictionary["ID"] = self.id
        purchaseIntentDictionary["Product"] = self.product.dictionaryRepresentation
        purchaseIntentDictionary["Timestamp"] = self.timestamp
        
        return purchaseIntentDictionary
    }
}


public struct AppCoinsSDKErrorData {
    public let type: String
    public let message: String
    public var description: String
    public let request: AppCoinsSDKErrorRequestData?
    
    init(error: AppCoinsSDKError) {
        switch error {
            case .networkError(let debugInfo):
                self.type = "networkError"
                self.message = debugInfo.message
                self.description = debugInfo.description
                if let request = debugInfo.request { AppCoinsSDKErrorRequestData(request: request) }
                    
            case .systemError(let debugInfo):
                self.type = "systemError"
                self.message = debugInfo.message
                self.description = debugInfo.description
                if let request = debugInfo.request { AppCoinsSDKErrorRequestData(request: request) }

            case .notEntitled(let debugInfo):
                self.type = "notEntitled"
                self.message = debugInfo.message
                self.description = debugInfo.description
                if let request = debugInfo.request { AppCoinsSDKErrorRequestData(request: request) }

            case .productUnavailable(let debugInfo):
                self.type = "productUnavailable"
                self.message = debugInfo.message
                self.description = debugInfo.description
                if let request = debugInfo.request { AppCoinsSDKErrorRequestData(request: request) }

            case .purchaseNotAllowed(let debugInfo):
                self.type = "purchaseNotAllowed"
                self.message = debugInfo.message
                self.description = debugInfo.description
                if let request = debugInfo.request { AppCoinsSDKErrorRequestData(request: request) }

            case .unknown(let debugInfo):
                self.type = "unknown"
                self.message = debugInfo.message
                self.description = debugInfo.description
                if let request = debugInfo.request { AppCoinsSDKErrorRequestData(request: request) }
        }
    }

    public struct AppCoinsSDKErrorRequestData {
        public let url: String
        public let method: String
        public let body: String
        public let responseData: String
        public let statusCode: Int
        
        init(request: DebugRequestInfo) {
            self.url = request.url
            self.method = request.method.rawValue
            self.body = request.body
            self.responseData = request.responseData
            self.statusCode = request.statusCode
        }
    }
    
    var dictionaryRepresentation: [String: Any] {
        var errorDictionary = [String: Any]()
        errorDictionary["Type"] = type
        errorDictionary["Message"] = message
        errorDictionary["Description"] = description
        
        var requestDictionary = [String: Any]()
        requestDictionary["URL"] = request.url
        requestDictionary["Method"] = request.method
        requestDictionary["Body"] = request.body
        requestDictionary["ResponseData"] = request.responseData
        requestDictionary["StatusCode"] = request.statusCode
        
        errorDictionary["Request"] = requestDictionary
        
        return errorDictionary
    }
}


@objcMembers
@objc public class UnityPlugin : NSObject {
    
    @objc public static let shared = UnityPlugin()

    @objc public func handleDeepLink(url: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            guard let urlObject = URL(string: url) else {
                let error: AppCoinsSDKError = .failed(message: "Invalid URL", description: "Invalid URL at UnityPlugin.swift:handleDeepLink"
                completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: error)])
                return
            }
            completion(["IsSuccess": true, "Value": AppcSDK.handle(redirectURL: urlObject)])
        }
    }

    @objc public func isAvailable(completion: @escaping ([String: Any]) -> Void) {
        Task {
            completion(["IsSuccess": true, "Value": await AppcSDK.isAvailable()])
        }
    }

    @objc public func getProducts(skus: [String], completion: @escaping ([String: Any]) -> Void) {
        Task {
            do {
                let products = try await (skus.isEmpty ? Product.products() : Product.products(for: skus))
                completion( ["IsSuccess": true, "Value": products.map { ProductData(product: $0).dictionaryRepresentation }] )
            } catch {
                guard let error = error as? AppCoinsSDKError else {
                    let unknownError: AppCoinsSDKError = .unknown(message: "Unknown Error", description: "Unknown Error at UnityPlugin.swift:getProducts")
                    completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: error)])
                    return
                }
                
                completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: error)])
            }
        }
    }

    @objc public func purchase(sku: String, payload: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            do {
                guard let product = try await Product.products(for: [sku]).first else {
                    let error: AppCoinsSDKError = .failed(message: "Product Not Found", description: "Product not found to perform purchase at UnityPlugin.swift:purchase")
                    completion(["Status": "failed", "Error": AppCoinsSDKErrorData(error: error)])
                }

                let result = await product.purchase(payload: payload.isEmpty ? nil : payload)
                
                let response: [String: Any] = {
                    switch result {
                    case .success(let verificationResult):
                        switch verificationResult {
                        case .verified(let purchase):
                            return [
                                "Status": "success",
                                "Value": [
                                    "VerificationResult": "verified",
                                    "Purchase": PurchaseData(purchase: purchase).dictionaryRepresentation
                                ]
                            ]
                        case .unverified(let purchase, let verificationError):
                            return [
                                "Status": "success",
                                "Value": [
                                    "VerificationResult": "unverified",
                                    "Purchase": PurchaseData(purchase: purchase).dictionaryRepresentation
                                    "VerificationError": verificationError.localizedDescription
                                ]
                            ]
                        }
                    case .pending:
                        return ["Status": "pending"]
                    case .userCancelled:
                        return ["Status": "user_cancelled"]
                    case .failed(let error):
                        ["Status": "failed", "Error": AppCoinsSDKErrorData(error: error)]
                    }
                }()
                
                completion(response)
            } catch {
                guard let error = error as? AppCoinsSDKError else {
                    let unknownError: AppCoinsSDKError = .unknown(message: "Unknown Error", description: "Unknown Error at UnityPlugin.swift:purchase")
                    completion(["Status": "failed", "Error": AppCoinsSDKErrorData(error: error)])
                    return
                }
                
                completion(["Status": "failed", "Error": AppCoinsSDKErrorData(error: error)])
            }
        }
    }
    
    @objc public func getAllPurchases(completion: @escaping ([[String: Any]]) -> Void) {
        Task {
            do {
                let purchases = try await Purchase.all()
                completion( ["IsSuccess": true, "Value": purchases.map { PurchaseData(purchase: $0).dictionaryRepresentation }] )
            } catch {
                guard let error = error as? AppCoinsSDKError else {
                    let unknownError: AppCoinsSDKError = .unknown(message: "Unknown Error", description: "Unknown Error at UnityPlugin.swift:getAllPurchases")
                    completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: error)])
                    return
                }
                
                completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: error)])
            }
        }
    }

    @objc public func getLatestPurchase(sku: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            do {
                let purchase = try await Purchase.latest(sku: sku)
                completion( ["IsSuccess": true, "Value": PurchaseData(purchase: purchase).dictionaryRepresentation] )
            } catch {
                guard let error = error as? AppCoinsSDKError else {
                    let unknownError: AppCoinsSDKError = .unknown(message: "Unknown Error", description: "Unknown Error at UnityPlugin.swift:getLatestPurchase")
                    completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: error)])
                    return
                }
                
                completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: error)])
            }
        }
    }

    @objc public func getUnfinishedPurchases(completion: @escaping ([[String: Any]]) -> Void) {
        Task {
            do {
                let purchases = try await Purchase.unfinished()
                completion( ["IsSuccess": true, "Value": purchases.map { PurchaseData(purchase: $0).dictionaryRepresentation }] )
            } catch {
                guard let error = error as? AppCoinsSDKError else {
                    let unknownError: AppCoinsSDKError = .unknown(message: "Unknown Error", description: "Unknown Error at UnityPlugin.swift:getUnfinishedPurchases")
                    completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: error)])
                    return
                }
                
                completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: error)])
            }
        }
    }

    @objc public func consumePurchase(sku: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            do {
                guard let purchase = try await Purchase.all().first(where: { $0.sku == sku && ($0.state == "ACKNOWLEDGED" || $0.state == "PENDING") }) else {
                    let purchaseError: AppCoinsSDKError = .failed(message: "Purchase Not Found", description: "Purchase not found when attempting to consume at UnityPlugin.swift:consumePurchase")
                    completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: purchaseError)])
                    return
                }
                
                try await purchase.finish()
                completion(["IsSuccess": true, "Value": true])
            } catch {
                guard let error = error as? AppCoinsSDKError else {
                    let unknownError: AppCoinsSDKError = .unknown(message: "Unknown Error", description: "Unknown Error at UnityPlugin.swift:consumePurchase")
                    completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: error)])
                    return
                }
                
                completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: error)])
            }
        }
    }
    
    @objc public func getTestingWalletAddress() -> String? {
        return Sandbox.getTestingWalletAddress()
    }
    
    @objc public func getPurchaseIntent(completion: @escaping ([String: Any]) -> Void) {
        guard let intent = Purchase.intent else {
            completion([:])
            return
        }
    
        completion(PurchaseIntentData(intent: intent).dictionaryRepresentation)
    }
    
    @objc public func confirmPurchaseIntent(payload: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            do {
                guard let intent = Purchase.intent else {
                    return completion(["State": "failed", "Error": "Intent not found", "Purchase": [:]])
                }

                let result = await intent.confirm(payload: payload.isEmpty ? nil : payload)
                
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
                        return ["State": "pending", "Error": "", "Purchase": [:]]
                    case .userCancelled:
                        return ["State": "user_cancelled", "Error": "", "Purchase": [:]]
                    case .failed(let error):
                        return ["State": "failed", "Error": error.localizedDescription, "Purchase": [:]]
                    }
                }()
                
                completion(response)
            } catch {
                completion(["State": "failed", "Error": error.localizedDescription, "Purchase": [:]])
            }
        }
    }
    
    @objc public func rejectPurchaseIntent() {
        guard let intent = Purchase.intent else {
            return
        }
        
        intent.reject()
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
            for await intent in Purchase.updates {
                
                // 1) Build a dictionary describing the intent
                let intentData: [String: Any] = PurchaseIntentData(intent: intent).dictionaryRepresentation
                
                // 2) Serialize that dictionary to JSON
                guard let cString = dictionaryToCString(intentData) else {
                    continue // if serialization fails, skip
                }

                // 3) Send to Unity (the third parameter must be a C-string)
                UnitySendMessageBridge("AppCoinsPurchaseManager", "OnPurchaseUpdatedInternal", cString)
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
