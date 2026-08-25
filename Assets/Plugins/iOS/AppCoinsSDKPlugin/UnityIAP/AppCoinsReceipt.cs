using System.Collections.Generic;
using AppCoins.Internal;

namespace AppCoins.Unity
{
    /// <summary>
    /// Builds the Unity IAP order receipt for an AppCoins purchase.
    ///
    /// Follows Unity's "unified receipt" shape ({ Store, TransactionID, Payload })
    /// where Payload is a JSON string carrying the AppCoins transaction and its
    /// local verification result. Developers can use the Payload to validate
    /// the purchase on their backend via the AppCoins Remote Check API.
    /// </summary>
    internal static class AppCoinsReceipt
    {
        public static string Build(Purchase purchase, string verificationResult)
        {
            if (purchase == null)
            {
                return string.Empty;
            }

            var payload = new Dictionary<string, object>
            {
                ["uid"]                = purchase.UID,
                ["sku"]                = purchase.Sku,
                ["created"]            = purchase.Created,
                ["payload"]            = purchase.Payload,
                ["verificationResult"] = verificationResult,
            };

            var root = new Dictionary<string, object>
            {
                ["Store"]         = AppCoinsIAP.AppCoinsStoreName,
                ["TransactionID"] = purchase.OrderUID,
                ["Payload"]       = MiniJsonAppCoins.Serialize(payload),
            };

            return MiniJsonAppCoins.Serialize(root);
        }
    }
}
