using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using AppCoins.Internal;

namespace AppCoins.Unity
{
    /// <summary>
    /// Public entry point for the AppCoins Unity IAP v5 integration.
    ///
    /// The AppCoins custom store is registered automatically on iOS at startup
    /// (see <see cref="AppCoinsIAPBootstrap"/>). Before using Unity IAP's
    /// StoreController, call <see cref="ConfigureStoreAsync"/> once to select
    /// which store backs purchases:
    ///
    /// <code>
    /// await AppCoinsIAP.ConfigureStoreAsync(AppCoinsStoreMode.Automatic);
    /// var controller = UnityIAPServices.StoreController();
    /// await controller.Connect();
    /// </code>
    /// </summary>
    public static class AppCoinsIAP
    {
        /// <summary>Registered name of the AppCoins custom store.</summary>
        public const string AppCoinsStoreName = "AppCoinsAppStore";

        /// <summary>Name of Unity IAP's built-in Apple App Store.</summary>
        public const string AppleStoreName = "AppleAppStore";

        /// <summary>
        /// The store selected by the most recent <see cref="ConfigureStoreAsync"/>
        /// call (either <see cref="AppCoinsStoreName"/> or <see cref="AppleStoreName"/>).
        /// </summary>
        public static string SelectedStore { get; private set; }

        internal static AppCoinsStore Store { get; private set; }

        private static bool _registered;

        /// <summary>
        /// Selects the active store according to <paramref name="mode"/> and sets
        /// it as Unity IAP's default. In <see cref="AppCoinsStoreMode.Automatic"/>
        /// the AppCoins <c>isAvailable</c> check decides between AppCoins Billing and Apple.
        /// Returns the chosen store name.
        /// </summary>
        public static async Task<string> ConfigureStoreAsync(AppCoinsStoreMode mode = AppCoinsStoreMode.Automatic)
        {
#if UNITY_IOS && !UNITY_EDITOR
            EnsureRegistered();

            string chosen;
            switch (mode)
            {
                case AppCoinsStoreMode.AppCoins:
                    chosen = AppCoinsStoreName;
                    break;
                case AppCoinsStoreMode.Apple:
                    chosen = AppleStoreName;
                    break;
                default: // Automatic
                    AppCoinsNativeBridge.Initialize();
                    bool available = await AppCoinsNativeBridge.IsAvailable();
                    chosen = available ? AppCoinsStoreName : AppleStoreName;
                    break;
            }

            UnityIAPServices.SetStoreAsDefault(chosen);
            SelectedStore = chosen;
            return chosen;
#else
            // AppCoins is iOS-only. On other platforms (and in the Editor) keep
            // Unity's normal platform default so nothing is disturbed.
            SelectedStore = UnityIAPServices.GetDefaultStore();
            return await Task.FromResult(SelectedStore);
#endif
        }

        /// <summary>
        /// Registers the AppCoins custom store with Unity IAP (idempotent).
        /// Called automatically on iOS at startup.
        /// </summary>
        internal static void EnsureRegistered()
        {
#if UNITY_IOS
            if (_registered) return;
            _registered = true;

            Store = new AppCoinsStore();
            UnityIAPServices.AddNewCustomStore(new AppCoinsStoreWrapper(AppCoinsStoreName, Store));
#endif
        }
    }
}
