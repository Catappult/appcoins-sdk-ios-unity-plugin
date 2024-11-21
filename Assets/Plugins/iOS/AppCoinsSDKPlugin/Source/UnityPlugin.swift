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
    public let verification: PurchaseVerification

    public struct PurchaseVerification {
        public let type: String
        public let data: PurchaseVerificationData
        public let signature: String
    }
    
    public struct PurchaseVerificationData: Codable {
        public let orderId: String
        public let packageName: String
        public let productId: String
        public let purchaseTime: Int
        public let purchaseToken: String
        public let purchaseState: Int
        public let developerPayload: String
    }
}

extension PurchaseData {
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

public struct AppCoinsPluginSDKError {
    public let errorType: String
    public let debugInfo: DebugInfo

    public struct DebugInfo {
        public let message: String
        public let description: String
        public let request: DebugRequestInfo
    }

    public struct DebugRequestInfo {
        public let url: String
        public let method: String
        public let responseData: String
        public let statusCode: String
    }

}

extension AppCoinsPluginSDKError {
    var appCoinsPluginSDKErrorDict: [String: Any] {
        let errorDict: [String: Any] = [
            "errorType": errorType,
            "debugInfo": [
                "message": debugInfo.message,
                "description": debugInfo.description,
                "request": [
                    "url": debugInfo.request.url,
                    "method": debugInfo.request.method,
                    "responseData": debugInfo.request.responseData,
                    "statusCode": debugInfo.request.statusCode
                ]
            ]
        ]
        return ["Error": errorDict]
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
                
                let arrayOfDictionaries = productItems.map { $0.dictionaryRepresentation }
                completion(arrayOfDictionaries)
            } catch let error as AppCoinsSDK.AppCoinsSDKError {
                switch error {
                    case .networkError(let debugInfo):
                        if let request = debugInfo.request {
                            let appCoinsPluginSDKError = AppCoinsPluginSDKError(
                                errorType: "networkError",
                                debugInfo: AppCoinsPluginSDKError.DebugInfo(
                                    message: debugInfo.message,
                                    description: debugInfo.description,
                                    request: AppCoinsPluginSDKError.DebugRequestInfo(
                                        url: request.url,
                                        method: request.method.rawValue,
                                        responseData: request.responseData,
                                        statusCode: String(request.statusCode)
                                    )
                                )
                            ).appCoinsPluginSDKErrorDict
                            completion([appCoinsPluginSDKError])
                        } else {
                            let appCoinsPluginSDKError = AppCoinsPluginSDKError(
                                errorType: "networkError",
                                debugInfo: AppCoinsPluginSDKError.DebugInfo(
                                    message: debugInfo.message,
                                    description: debugInfo.description,
                                    request: AppCoinsPluginSDKError.DebugRequestInfo(
                                        url: "",
                                        method: "",
                                        responseData: "",
                                        statusCode: ""
                                    )
                                )
                            ).appCoinsPluginSDKErrorDict
                            completion([appCoinsPluginSDKError])
                        }
                    case .systemError(let debugInfo):
                        if let request = debugInfo.request {
                            let appCoinsPluginSDKError = AppCoinsPluginSDKError(
                                errorType: "systemError",
                                debugInfo: AppCoinsPluginSDKError.DebugInfo(
                                    message: debugInfo.message,
                                    description: debugInfo.description,
                                    request: AppCoinsPluginSDKError.DebugRequestInfo(
                                        url: request.url,
                                        method: request.method.rawValue,
                                        responseData: request.responseData,
                                        statusCode: String(request.statusCode)
                                    )
                                )
                            ).appCoinsPluginSDKErrorDict
                            completion([appCoinsPluginSDKError])
                        } else {
                            let appCoinsPluginSDKError = AppCoinsPluginSDKError(
                                errorType: "systemError",
                                debugInfo: AppCoinsPluginSDKError.DebugInfo(
                                    message: debugInfo.message,
                                    description: debugInfo.description,
                                    request: AppCoinsPluginSDKError.DebugRequestInfo(
                                        url: "",
                                        method: "",
                                        responseData: "",
                                        statusCode: ""
                                    )
                                )
                            ).appCoinsPluginSDKErrorDict
                            completion([appCoinsPluginSDKError])
                        }
                    case .notEntitled(let debugInfo):
                        if let request = debugInfo.request {
                            let appCoinsPluginSDKError = AppCoinsPluginSDKError(
                                errorType: "notEntitled",
                                debugInfo: AppCoinsPluginSDKError.DebugInfo(
                                    message: debugInfo.message,
                                    description: debugInfo.description,
                                    request: AppCoinsPluginSDKError.DebugRequestInfo(
                                        url: request.url,
                                        method: request.method.rawValue,
                                        responseData: request.responseData,
                                        statusCode: String(request.statusCode)
                                    )
                                )
                            ).appCoinsPluginSDKErrorDict
                            completion([appCoinsPluginSDKError])
                        } else {
                            let appCoinsPluginSDKError = AppCoinsPluginSDKError(
                                errorType: "notEntitled",
                                debugInfo: AppCoinsPluginSDKError.DebugInfo(
                                    message: debugInfo.message,
                                    description: debugInfo.description,
                                    request: AppCoinsPluginSDKError.DebugRequestInfo(
                                        url: "",
                                        method: "",
                                        responseData: "",
                                        statusCode: ""
                                    )
                                )
                            ).appCoinsPluginSDKErrorDict
                            completion([appCoinsPluginSDKError])
                        }
                    case .productUnavailable(let debugInfo):
                        if let request = debugInfo.request {
                            let appCoinsPluginSDKError = AppCoinsPluginSDKError(
                                errorType: "productUnavailable",
                                debugInfo: AppCoinsPluginSDKError.DebugInfo(
                                    message: debugInfo.message,
                                    description: debugInfo.description,
                                    request: AppCoinsPluginSDKError.DebugRequestInfo(
                                        url: request.url,
                                        method: request.method.rawValue,
                                        responseData: request.responseData,
                                        statusCode: String(request.statusCode)
                                    )
                                )
                            ).appCoinsPluginSDKErrorDict
                            completion([appCoinsPluginSDKError])
                        } else {
                            let appCoinsPluginSDKError = AppCoinsPluginSDKError(
                                errorType: "productUnavailable",
                                debugInfo: AppCoinsPluginSDKError.DebugInfo(
                                    message: debugInfo.message,
                                    description: debugInfo.description,
                                    request: AppCoinsPluginSDKError.DebugRequestInfo(
                                        url: "",
                                        method: "",
                                        responseData: "",
                                        statusCode: ""
                                    )
                                )
                            ).appCoinsPluginSDKErrorDict
                            completion([appCoinsPluginSDKError])
                        }
                    case .purchaseNotAllowed(let debugInfo):
                        if let request = debugInfo.request {
                            let appCoinsPluginSDKError = AppCoinsPluginSDKError(
                                errorType: "purchaseNotAllowed",
                                debugInfo: AppCoinsPluginSDKError.DebugInfo(
                                    message: debugInfo.message,
                                    description: debugInfo.description,
                                    request: AppCoinsPluginSDKError.DebugRequestInfo(
                                        url: request.url,
                                        method: request.method.rawValue,
                                        responseData: request.responseData,
                                        statusCode: String(request.statusCode)
                                    )
                                )
                            ).appCoinsPluginSDKErrorDict
                            completion([appCoinsPluginSDKError])
                        } else {
                            let appCoinsPluginSDKError = AppCoinsPluginSDKError(
                                errorType: "purchaseNotAllowed",
                                debugInfo: AppCoinsPluginSDKError.DebugInfo(
                                    message: debugInfo.message,
                                    description: debugInfo.description,
                                    request: AppCoinsPluginSDKError.DebugRequestInfo(
                                        url: "",
                                        method: "",
                                        responseData: "",
                                        statusCode: ""
                                    )
                                )
                            ).appCoinsPluginSDKErrorDict
                            completion([appCoinsPluginSDKError])
                        }
                    case .unknown(let debugInfo):
                        if let request = debugInfo.request {
                            let appCoinsPluginSDKError = AppCoinsPluginSDKError(
                                errorType: "unknown",
                                debugInfo: AppCoinsPluginSDKError.DebugInfo(
                                    message: debugInfo.message,
                                    description: debugInfo.description,
                                    request: AppCoinsPluginSDKError.DebugRequestInfo(
                                        url: request.url,
                                        method: request.method.rawValue,
                                        responseData: request.responseData,
                                        statusCode: String(request.statusCode)
                                    )
                                )
                            ).appCoinsPluginSDKErrorDict
                            completion([appCoinsPluginSDKError])
                        } else {
                            let appCoinsPluginSDKError = AppCoinsPluginSDKError(
                                errorType: "unknown",
                                debugInfo: AppCoinsPluginSDKError.DebugInfo(
                                    message: debugInfo.message,
                                    description: debugInfo.description,
                                    request: AppCoinsPluginSDKError.DebugRequestInfo(
                                        url: "",
                                        method: "",
                                        responseData: "",
                                        statusCode: ""
                                    )
                                )
                            ).appCoinsPluginSDKErrorDict
                            completion([appCoinsPluginSDKError])
                        }
                }
            }
        }
    }

    @objc public func purchase(sku: String, payload: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            let products = try await Product.products(for: [sku])

            let payloadToSend = payload.isEmpty ? nil : payload
            let result = await products.first?.purchase(payload: payloadToSend)
            
            switch result {
                case .success(let verificationResult):
                     switch verificationResult {
                           case .verified(let purchase):
                                let state = "success"
                                
                                let purchaseData = PurchaseData(
                                    uid: purchase.uid,
                                    sku: purchase.sku,
                                    state: purchase.state,
                                    orderUid: purchase.orderUid,
                                    payload: purchase.payload,
                                    created: purchase.created,
                                    verification: PurchaseData.PurchaseVerification(
                                        type: purchase.verification.type,
                                        data: PurchaseData.PurchaseVerificationData(
                                            orderId: purchase.verification.data.orderId,
                                            packageName: purchase.verification.data.packageName,
                                            productId: purchase.verification.data.productId,
                                            purchaseTime: purchase.verification.data.purchaseTime,
                                            purchaseToken: purchase.verification.data.purchaseToken,
                                            purchaseState: purchase.verification.data.purchaseState,
                                            developerPayload: purchase.verification.data.developerPayload
                                        ),
                                        signature: purchase.verification.signature
                                    )
                                ).dictionaryRepresentation
                         
                                completion(["State": state, "Error": "", "Purchase": purchaseData])
                         
                           case .unverified(let purchase, let verificationError):
                                let state = "unverified"
                                let errorMessage = verificationError.localizedDescription
                         
                                let purchaseData = PurchaseData(
                                     uid: purchase.uid,
                                     sku: purchase.sku,
                                     state: purchase.state,
                                     orderUid: purchase.orderUid,
                                     payload: purchase.payload,
                                     created: purchase.created,
                                     verification: PurchaseData.PurchaseVerification(
                                         type: purchase.verification.type,
                                         data: PurchaseData.PurchaseVerificationData(
                                             orderId: purchase.verification.data.orderId,
                                             packageName: purchase.verification.data.packageName,
                                             productId: purchase.verification.data.productId,
                                             purchaseTime: purchase.verification.data.purchaseTime,
                                             purchaseToken: purchase.verification.data.purchaseToken,
                                             purchaseState: purchase.verification.data.purchaseState,
                                             developerPayload: purchase.verification.data.developerPayload
                                         ),
                                         signature: purchase.verification.signature
                                     )
                                ).dictionaryRepresentation
                                
                                completion(["State": state, "Error": errorMessage, "Purchase": purchaseData])
                     }
                case .pending:
                    let state = "pending"
                    completion(["State": state, "Error": "", "Purchase": ""])
                case .userCancelled:
                    let state = "user_cancelled"
                    completion(["State": state, "Error": "", "Purchase": ""])
                case .failed(let error):
                    let state = "failed"
                    let errorMessage = error.localizedDescription
                    completion(["State": state, "Error": errorMessage, "Purchase": ""])
                case .none:
                    let state = "none"
                    completion(["State": state, "Error": "", "Purchase": ""])
                }
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
                        created: purchase.created,
                        verification: PurchaseData.PurchaseVerification(
                            type: purchase.verification.type,
                            data: PurchaseData.PurchaseVerificationData(
                                orderId: purchase.verification.data.orderId,
                                packageName: purchase.verification.data.packageName,
                                productId: purchase.verification.data.productId,
                                purchaseTime: purchase.verification.data.purchaseTime,
                                purchaseToken: purchase.verification.data.purchaseToken,
                                purchaseState: purchase.verification.data.purchaseState,
                                developerPayload: purchase.verification.data.developerPayload
                            ),
                            signature: purchase.verification.signature
                        )
                    )
                }
                
                let arrayOfDictionaries = purchaseItems.map { $0.dictionaryRepresentation }
                completion(arrayOfDictionaries)
            } catch {
                completion([])
            }
        }
    }

    @objc public func getLatestPurchase(sku: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            let purchase = try? await Purchase.latest(sku: sku)
            let purchaseItem = PurchaseData(
                uid: purchase?.uid ?? "",
                sku: purchase?.sku ?? "",
                state: purchase?.state ?? "",
                orderUid: purchase?.orderUid ?? "",
                payload: purchase?.payload ?? "",
                created: purchase?.created ?? "",
                verification: PurchaseData.PurchaseVerification(
                    type: purchase?.verification.type ?? "",
                    data: PurchaseData.PurchaseVerificationData(
                        orderId: purchase?.verification.data.orderId ?? "",
                        packageName: purchase?.verification.data.packageName ?? "",
                        productId: purchase?.verification.data.productId ?? "",
                        purchaseTime: purchase?.verification.data.purchaseTime ?? 0,
                        purchaseToken: purchase?.verification.data.purchaseToken ?? "",
                        purchaseState: purchase?.verification.data.purchaseState ?? 0,
                        developerPayload: purchase?.verification.data.developerPayload ?? ""
                    ),
                    signature: purchase?.verification.signature ?? ""
                )
            )
            completion(purchaseItem.dictionaryRepresentation)
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
                        created: purchase.created,
                        verification: PurchaseData.PurchaseVerification(
                            type: purchase.verification.type,
                            data: PurchaseData.PurchaseVerificationData(
                                orderId: purchase.verification.data.orderId,
                                packageName: purchase.verification.data.packageName,
                                productId: purchase.verification.data.productId,
                                purchaseTime: purchase.verification.data.purchaseTime,
                                purchaseToken: purchase.verification.data.purchaseToken,
                                purchaseState: purchase.verification.data.purchaseState,
                                developerPayload: purchase.verification.data.developerPayload
                            ),
                            signature: purchase.verification.signature
                        )
                    )
                }
                
                let arrayOfDictionaries = purchaseItems.map { $0.dictionaryRepresentation }
                completion(arrayOfDictionaries)
            } catch {
                completion([])
            }
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
    
    @objc public func getTestingWalletAddress() -> String? {
        return Sandbox.getTestingWalletAddress()
    }
}
