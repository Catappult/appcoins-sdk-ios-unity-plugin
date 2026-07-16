namespace AppCoins.Unity
{
    /// <summary>
    /// How the AppCoins Unity IAP integration should pick the active store.
    /// </summary>
    public enum AppCoinsStoreMode
    {
        /// <summary>
        /// Use the AppCoins SDK when it reports itself available on the device
        /// (iOS 17.4+ / alternative distribution); otherwise fall back to the
        /// built-in Apple App Store. This is the recommended default.
        /// </summary>
        Automatic,

        /// <summary>Always use the AppCoins Billing store.</summary>
        AppCoins,

        /// <summary>Always use the built-in Apple App Store.</summary>
        Apple,
    }
}
