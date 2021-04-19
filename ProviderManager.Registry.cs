using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;

namespace PriceFeedService
{
    partial class ProviderManager
    {
        public static bool RegisterProvider(UInt160 provider)
        {
            if (!IsOwner()) throw new Exception("No authorization");
            if (Providers[provider] is not null) throw new InvalidOperationException("The provider already exists.");
            Providers.Put(provider, 0);
            return true;
        }

        public static bool UnRegisterProvider(UInt160 provider)
        {
            if (!IsOwner()) throw new Exception("No authorization");
            if (Providers[provider] is null) throw new InvalidOperationException("The provider does not exist.");
            Providers.Delete(provider);
            return true;
        }

        public static UInt160[] GetRegisteredProviders()
        {
            List<UInt160> ret = new();
            Iterator providersList = Providers.Find(FindOptions.RemovePrefix | FindOptions.KeysOnly);
            while (providersList.Next())
            {
                ret.Add((UInt160)providersList.Value);
            }
            return ret;

        }
    }
}
