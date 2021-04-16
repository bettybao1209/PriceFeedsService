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
            if (!Runtime.CheckWitness(Owner)) throw new Exception("No authorization");
            ProviderStatus status = ByteString2ProviderStatus(Providers.Get(provider));
            if (status == ProviderStatus.Registered) throw new Exception("Provider already registered");
            byte[] registered = IssuerStatus2ByteArray(ProviderStatus.Registered);
            Providers.Put(provider, (ByteString)registered);
            return true;
        }

        public static bool UnRegisterProvider(UInt160 provider)
        {
            if (!Runtime.CheckWitness(Owner)) throw new Exception("No authorization");
            ProviderStatus status = ByteString2ProviderStatus(Providers.Get(provider));
            if (status == ProviderStatus.NotRegistered) throw new Exception("No such provider registered");
            Providers.Delete(provider);
            return true;
        }

        public static UInt160[] GetRegisteredProviders()
        {
            List<UInt160> ret = new();
            Iterator providersList = Storage.Find(Storage.CurrentContext, new byte[] { Prefix_Providers }, FindOptions.RemovePrefix | FindOptions.KeysOnly);
            while (providersList.Next())
            {
                ret.Add((UInt160)providersList.Value);
            }
            return ret;

        }
    }
}
