using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace AppCoins.Unity
{
    /// <summary>
    /// AppCoins-backed implementation of Unity IAP's <see cref="IOrderInfo"/>.
    ///
    /// Implementing the public interface directly lets the custom store hand
    /// back <see cref="PendingOrder"/> / <see cref="ConfirmedOrder"/> objects
    /// without touching Unity's internal <c>OrderInfo</c> type. The
    /// <see cref="Receipt"/> carries the AppCoins signature verification so
    /// developers can server-validate through Remote Check.
    /// </summary>
    public class AppCoinsOrderInfo : IOrderInfo
    {
        public AppCoinsOrderInfo(string receipt, string transactionId)
        {
            Receipt = receipt ?? string.Empty;
            TransactionID = transactionId ?? string.Empty;
            PurchasedProductInfo = new List<IPurchasedProductInfo>();
        }

        // AppCoins is neither Apple nor Google native billing.
        public IAppleOrderInfo Apple => null;
        public IGoogleOrderInfo Google => null;

        // Populated by Unity's PendingOrder/ConfirmedOrder constructors.
        public List<IPurchasedProductInfo> PurchasedProductInfo { get; set; }

        public string Receipt { get; }
        public string TransactionID { get; }
    }
}
