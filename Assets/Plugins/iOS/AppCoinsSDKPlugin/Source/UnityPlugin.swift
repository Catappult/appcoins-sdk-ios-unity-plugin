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
        self.sku = product.id
        self.title = product.displayName
        self.description = product.description
        self.priceCurrency = ""
        self.priceValue = "\(product.price)"
        self.priceLabel = product.displayPrice
        self.priceSymbol = ""
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


public struct TransactionData {
    public let uid: String
    public let sku: String
    public let orderUid: String
    public let payload: String?
    public let created: String

    private static let isoFormatter: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return f
    }()

    init(transaction: Transaction) {
        self.uid = transaction.id
        self.sku = transaction.productID
        self.orderUid = transaction.id
        self.payload = transaction.appAccountToken?.uuidString
        self.created = TransactionData.isoFormatter.string(from: transaction.purchaseDate)
    }

    var dictionaryRepresentation: [String: Any] {
        var dict = [String: Any]()
        dict["UID"] = uid
        dict["Sku"] = sku
        dict["State"] = "PENDING"
        dict["OrderUID"] = orderUid
        dict["Payload"] = payload
        dict["Created"] = created
        dict["Verification"] = [String: Any]()
        return dict
    }
}


public struct AppCoinsSDKErrorData {
    public let type: String
    public let message: String
    public var description: String
    public let request: AppCoinsSDKErrorRequestData?

    init(error: AppCoinsSDKError) {
        let (t, d): (String, String) = {
            switch error {
            case .networkError(let d):       return ("networkError", d)
            case .systemError(let d):        return ("systemError", d)
            case .notEntitled(let d):        return ("notEntitled", d)
            case .productUnavailable(let d): return ("productUnavailable", d)
            case .purchaseNotAllowed(let d): return ("purchaseNotAllowed", d)
            case .unknown(let d):            return ("unknown", d)
            }
        }()
        self.type = t

        // The description is JSON produced by DebugInfo.format(); parse it to
        // extract structured fields for the C# side.
        if let data = d.data(using: .utf8),
           let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any] {
            self.message = json["message"] as? String ?? ""
            self.description = json["description"] as? String ?? d
            if let req = json["request"] as? [String: Any] {
                self.request = AppCoinsSDKErrorRequestData(
                    url: req["url"] as? String ?? "",
                    method: req["method"] as? String ?? "",
                    body: req["body"] as? String ?? "",
                    responseData: req["responseData"] as? String ?? "",
                    statusCode: req["statusCode"] as? Int ?? 0
                )
            } else {
                self.request = nil
            }
        } else {
            self.message = ""
            self.description = d
            self.request = nil
        }
    }

    public struct AppCoinsSDKErrorRequestData {
        public let url: String
        public let method: String
        public let body: String
        public let responseData: String
        public let statusCode: Int
    }

    var dictionaryRepresentation: [String: Any] {
        var errorDictionary = [String: Any]()
        errorDictionary["Type"] = type
        errorDictionary["Message"] = message
        errorDictionary["Description"] = description

        if let request = request {
            var requestDictionary = [String: Any]()
            requestDictionary["URL"] = request.url
            requestDictionary["Method"] = request.method
            requestDictionary["Body"] = request.body
            requestDictionary["ResponseData"] = request.responseData
            requestDictionary["StatusCode"] = request.statusCode
            errorDictionary["Request"] = requestDictionary
        }

        return errorDictionary
    }
}


@objcMembers
@objc public class UnityPlugin : NSObject {

    @objc public static let shared = UnityPlugin()

    @objc public func initialize() {
        AppcSDK.initialize()
    }

    @objc public func handleDeepLink(url: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            guard let urlObject = URL(string: url) else {
                let invalidURLError: AppCoinsSDKError = .systemError("Invalid URL at UnityPlugin.swift:handleDeepLink")
                completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: invalidURLError).dictionaryRepresentation])
                return
            }
            completion(["IsSuccess": AppcSDK.handle(redirectURL: urlObject)])
        }
    }

    @objc public func isAvailable(completion: @escaping ([String: Any]) -> Void) {
        Task {
            completion(["IsSuccess": await AppcSDK.isAvailable()])
        }
    }

    @objc public func getProducts(skus: [String], completion: @escaping ([String: Any]) -> Void) {
        Task {
            do {
                let products = try await Product.products(for: skus)
                completion(["IsSuccess": true, "Value": products.map { ProductData(product: $0).dictionaryRepresentation }])
            } catch {
                guard let sdkError = error as? AppCoinsSDKError else {
                    let unknownError: AppCoinsSDKError = .unknown("Unknown Error at UnityPlugin.swift:getProducts")
                    completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: unknownError).dictionaryRepresentation])
                    return
                }
                completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: sdkError).dictionaryRepresentation])
            }
        }
    }

    @objc public func purchase(sku: String, payload: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            do {
                guard let product = try await Product.products(for: [sku]).first else {
                    let error: AppCoinsSDKError = .systemError("Product not found to perform purchase at UnityPlugin.swift:purchase")
                    completion(["State": "failed", "Error": AppCoinsSDKErrorData(error: error).dictionaryRepresentation])
                    return
                }

                var options: Set<Product.PurchaseOption> = []
                if !payload.isEmpty, let uuid = UUID(uuidString: payload) {
                    options.insert(Product.PurchaseOption.appAccountToken(uuid))
                }

                let result = try await product.purchase(options: options)

                let response: [String: Any] = {
                    switch result {
                    case .success(let verificationResult):
                        switch verificationResult {
                        case .verified(let transaction):
                            return [
                                "State": "success",
                                "Value": [
                                    "VerificationResult": "verified",
                                    "Purchase": TransactionData(transaction: transaction).dictionaryRepresentation
                                ]
                            ]
                        case .unverified(let transaction, let verificationError):
                            return [
                                "State": "success",
                                "Value": [
                                    "VerificationResult": "unverified",
                                    "Purchase": TransactionData(transaction: transaction).dictionaryRepresentation,
                                    "VerificationError": AppCoinsSDKErrorData(error: verificationError).dictionaryRepresentation
                                ]
                            ]
                        }
                    case .pending:
                        return ["State": "pending"]
                    case .userCancelled:
                        return ["State": "user_cancelled"]
                    }
                }()

                completion(response)
            } catch {
                guard let sdkError = error as? AppCoinsSDKError else {
                    let unknownError: AppCoinsSDKError = .unknown("Unknown Error at UnityPlugin.swift:purchase")
                    completion(["State": "failed", "Error": AppCoinsSDKErrorData(error: unknownError).dictionaryRepresentation])
                    return
                }
                completion(["State": "failed", "Error": AppCoinsSDKErrorData(error: sdkError).dictionaryRepresentation])
            }
        }
    }

    @objc public func getAllPurchases(completion: @escaping ([String: Any]) -> Void) {
        Task {
            var transactions: [TransactionData] = []
            for await result in Transaction.all {
                switch result {
                case .verified(let t):      transactions.append(TransactionData(transaction: t))
                case .unverified(let t, _): transactions.append(TransactionData(transaction: t))
                }
            }
            completion(["IsSuccess": true, "Value": transactions.map { $0.dictionaryRepresentation }])
        }
    }

    @objc public func getLatestPurchase(sku: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            var latestTransaction: Transaction?
            for await result in Transaction.unfinished {
                if case .verified(let t) = result, t.productID == sku {
                    latestTransaction = t
                    break
                }
            }
            if let t = latestTransaction {
                completion(["IsSuccess": true, "Value": TransactionData(transaction: t).dictionaryRepresentation])
            } else {
                completion(["IsSuccess": true])
            }
        }
    }

    @objc public func getUnfinishedPurchases(completion: @escaping ([String: Any]) -> Void) {
        Task {
            var transactions: [TransactionData] = []
            for await result in Transaction.unfinished {
                switch result {
                case .verified(let t):      transactions.append(TransactionData(transaction: t))
                case .unverified(let t, _): transactions.append(TransactionData(transaction: t))
                }
            }
            completion(["IsSuccess": true, "Value": transactions.map { $0.dictionaryRepresentation }])
        }
    }

    @objc public func consumePurchase(sku: String, completion: @escaping ([String: Any]) -> Void) {
        Task {
            var targetTransaction: Transaction?
            for await result in Transaction.unfinished {
                if case .verified(let t) = result, t.productID == sku {
                    targetTransaction = t
                    break
                }
            }

            guard let transaction = targetTransaction else {
                let purchaseError: AppCoinsSDKError = .systemError("Purchase not found when attempting to consume at UnityPlugin.swift:consumePurchase")
                completion(["IsSuccess": false, "Error": AppCoinsSDKErrorData(error: purchaseError).dictionaryRepresentation])
                return
            }

            await transaction.finish()
            completion(["IsSuccess": true])
        }
    }

    @objc public func getTestingWalletAddress(completion: @escaping ([String: Any]) -> Void) {
        Task {
            let address = await Sandbox.getTestingWalletAddress()
            completion(["IsSuccess": true, "Value": address ?? ""])
        }
    }
}
