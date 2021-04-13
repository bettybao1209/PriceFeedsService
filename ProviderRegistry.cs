using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.Numerics;

namespace PriceFeedService
{
    public enum ProviderStatus
    {
        NotRegistered = 0,
        Registered = 1
    }

    public class ProviderRegister : SmartContract
    {
        private static StorageMap providers => Storage.CurrentContext.CreateMap(nameof(providers));
        private static StorageMap price => Storage.CurrentContext.CreateMap(nameof(price));
        private static readonly UInt160 Owner = "NLq7pkzkWi1eZLi1thgm36KbGg6HYTM8Jv".ToScriptHash(); // Changed

        public static UInt160 RegistryHash => Owner;
        public static bool RegisterProvider(UInt160 provider)
        {
            if (!Runtime.CheckWitness(Owner)) throw new Exception("Unauthorized!");
            ProviderStatus status = ByteArray2ProviderStatus((byte[])providers.Get(provider));
            if (status == ProviderStatus.Registered) throw new Exception("Provider already registered");
            byte[] registered = IssuerStatus2ByteArray(ProviderStatus.Registered);
            providers.Put(provider, (ByteString)registered);
            return true;
        }

        public static UInt160[] GetRegisteredProviders()
        {
            List<UInt160> ret = new List<UInt160>();
            Iterator providersList = Storage.Find(Storage.CurrentContext, nameof(providers), FindOptions.RemovePrefix | FindOptions.KeysOnly);
            while (providersList.Next())
            {
                ret.Add((UInt160)providersList.Value);
            }
            return ret;
        }

        public static bool UnRegisterProvider(UInt160 provider)
        {
            if (!Runtime.CheckWitness(Owner)) throw new Exception("Unauthorized!");
            ProviderStatus status = ByteArray2ProviderStatus((byte[])providers.Get(provider));
            if (status == ProviderStatus.NotRegistered) throw new Exception("No such provider registered");
            providers.Delete(provider);
            return true;
        }

        public static void UpdatePrice(string symbol, string currentPrice)
        {
            UInt160 provider = ExecutionEngine.CallingScriptHash;
            ProviderStatus status = ByteArray2ProviderStatus((byte[])providers.Get(provider));
            if (status == ProviderStatus.NotRegistered) throw new Exception("No such provider registered");
            StorageMap priceList = Storage.CurrentContext.CreateMap(StdLib.Serialize(provider));
            priceList.Put(symbol, currentPrice);
        }

        public static object GetLatestPrice(string symbol)
        {
            Map<UInt160, string> priceMap = new Map<UInt160, string>();
            UInt160[] registeredProvider = GetRegisteredProviders();
            foreach (UInt160 key in registeredProvider)
            {
                StorageMap priceList = Storage.CurrentContext.CreateMap(key);
                priceMap[key] = priceList.Get(symbol);
            }
            return priceMap;
        }

        public static object GetLatestPriceWithProvider(UInt160[] requiredProviders, string symbol)
        {
            Map<UInt160, string> priceMap = new Map<UInt160, string>();
            if (providers != null && requiredProviders.Length > 0)
            {
                foreach (UInt160 key in requiredProviders)
                {
                    ProviderStatus status = ByteArray2ProviderStatus((byte[])providers.Get(key));
                    if (status == ProviderStatus.Registered)
                    {
                        StorageMap priceList = Storage.CurrentContext.CreateMap(key);
                        priceMap[key] = priceList.Get(symbol);
                    }
                }
                return priceMap;
            }
            return GetLatestPrice(symbol);
        }

        public static void Update(ByteString nefFile, string manifest, object data)
        {
            if (!IsOwner()) throw new Exception("No authorization.");
            ContractManagement.Update(nefFile, manifest, data);
        }

        public static void Destroy()
        {
            if (!IsOwner()) throw new Exception("No authorization.");
            ContractManagement.Destroy();
        }

        private static bool IsOwner() => Runtime.CheckWitness(Owner);
        private static byte[] IssuerStatus2ByteArray(ProviderStatus value) => ((BigInteger)(int)value).ToByteArray();

        private static ProviderStatus ByteArray2ProviderStatus(byte[] value)
        {
            if (value == null || value.Length == 0) return ProviderStatus.NotRegistered;
            return (ProviderStatus)(int)value.ToBigInteger();
        }

        public static int test()
        {
            return 2;
        }
    }
}
