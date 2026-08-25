using UnityEngine;

namespace AppCoins.Unity
{
    /// <summary>
    /// Registers the AppCoins custom store with Unity IAP automatically on iOS.
    ///
    /// Registration only makes the store available — it does NOT change the
    /// default store. The developer chooses which store is active by calling
    /// <see cref="AppCoinsIAP.ConfigureStoreAsync"/>. Until then, Unity keeps
    /// its normal platform default (the Apple App Store on iOS), so non-iOS
    /// platforms are entirely unaffected.
    /// </summary>
    public static class AppCoinsIAPBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if UNITY_IOS && !UNITY_EDITOR
            AppCoinsIAP.EnsureRegistered();
#endif
        }
    }
}
