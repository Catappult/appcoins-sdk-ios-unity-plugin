using System.Collections.Generic;
using AppCoins.Internal;

namespace AppCoins.Unity
{
    /// <summary>
    /// Builds the Unity IAP order receipt for an AppCoins purchase.
    ///
    /// Follows Unity's "unified receipt" shape ({ Store, TransactionID, Payload })
    /// where Payload is a JSON string carrying the AppCoins purchase and its
    /// signature verification. Developers can forward the Payload to their
    /// backend to validate the signature (Remote Check).
    /// </summary>
    internal static class AppCoinsReceipt
    {
        public static string Build(Purchase purchase, string verificationResult)
        {
            if (purchase == null)
            {
                return string.Empty;
            }

            var verification = new Dictionary<string, object>();
            if (purchase.Verification != null)
            {
                verification["type"] = purchase.Verification.Type;
                verification["signature"] = purchase.Verification.Signature;
                if (purchase.Verification.Data != null)
                {
                    var data = purchase.Verification.Data;
                    verification["data"] = new Dictionary<string, object>
                    {
                        ["orderId"] = data.OrderId,
                        ["packageName"] = data.PackageName,
                        ["productId"] = data.ProductId,
                        ["purchaseTime"] = data.PurchaseTime,
                        ["purchaseToken"] = data.PurchaseToken,
                        ["purchaseState"] = data.PurchaseState,
                        ["developerPayload"] = data.DeveloperPayload,
                    };
                }
            }

            var payload = new Dictionary<string, object>
            {
                ["uid"] = purchase.UID,
                ["sku"] = purchase.Sku,
                ["state"] = purchase.State,
                ["orderUid"] = purchase.OrderUID,
                ["payload"] = purchase.Payload,
                ["created"] = purchase.Created,
                ["verificationResult"] = verificationResult,
                ["verification"] = verification,
            };

            var root = new Dictionary<string, object>
            {
                ["Store"] = AppCoinsIAP.AptoideStoreName,
                ["TransactionID"] = purchase.OrderUID,
                ["Payload"] = MiniJsonAppCoins.Serialize(payload),
            };

            return MiniJsonAppCoins.Serialize(root);
        }
    }
}
