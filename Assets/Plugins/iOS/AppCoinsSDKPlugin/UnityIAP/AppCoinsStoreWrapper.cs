using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace AppCoins.Unity
{
    /// <summary>
    /// Wraps <see cref="AppCoinsStore"/> for registration with Unity IAP via
    /// <c>UnityIAPServices.AddNewCustomStore(IStoreWrapper)</c>. The built-in
    /// StoreWrapper is internal to the package, so custom stores supply their
    /// own trivial implementation.
    /// </summary>
    public class AppCoinsStoreWrapper : IStoreWrapper
    {
        private readonly AppCoinsStore _store;

        public AppCoinsStoreWrapper(string name, AppCoinsStore store)
        {
            this.name = name;
            _store = store;
        }

        public Store instance => _store;

        public string name { get; }

        public ConnectionState GetStoreConnectionState() => _store.ConnectionState;
    }
}
