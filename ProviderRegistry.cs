using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.Numerics;

namespace PriceFeedService
{
    public enum ProviderStatus
    {
        NotRegistered = 0,
        Registered = 1
    }

    [ContractPermission("0xfffdc93764dbaddd97c48f252a53ea4643faa3fd", "destroy", "update")]
    public class ProviderRegistry : SmartContract
    {
        private static StorageMap providers => new StorageMap(Storage.CurrentContext, nameof(providers));

        [InitialValue("NWhJATyChXvaBqS9debbk47Uf2X33WtHtL", ContractParameterType.Hash160)]
        private static readonly UInt160 Owner = default; //  Replace it with your own address

        public static bool RegisterProvider(UInt160 provider)
        {
            if (!Runtime.CheckWitness(Owner)) throw new Exception("No authorization");
            ProviderStatus status = ByteString2ProviderStatus(providers.Get(provider));
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
            if (!Runtime.CheckWitness(Owner)) throw new Exception("No authorization");
            ProviderStatus status = ByteString2ProviderStatus(providers.Get(provider));
            if (status == ProviderStatus.NotRegistered) throw new Exception("No such provider registered");
            providers.Delete(provider);
            return true;
        }

        public static void UpdatePrice(string symbol, string currentPrice)
        {
            UInt160 provider = Runtime.CallingScriptHash;
            ProviderStatus status = ByteString2ProviderStatus(providers.Get(provider));
            if (status == ProviderStatus.NotRegistered) throw new Exception("No such provider registered");
            StorageMap priceList = new StorageMap(Storage.CurrentContext, provider);
            priceList.Put(symbol, currentPrice);
        }

        public static object GetLatestPrice(string symbol, UInt160[] requiredProviders = null)
        {
            Map<UInt160, string> priceMap = new Map<UInt160, string>();
            if (providers != null && requiredProviders.Length > 0)
            {
                foreach (UInt160 key in requiredProviders)
                {
                    ProviderStatus status = ByteString2ProviderStatus(providers.Get(key));
                    if (status == ProviderStatus.Registered)
                    {
                        StorageMap priceList = new StorageMap(Storage.CurrentContext, key);
                        priceMap[key] = priceList.Get(symbol);
                    }
                }
            }
            else
            {
                UInt160[] registeredProvider = GetRegisteredProviders();
                foreach (UInt160 key in registeredProvider)
                {
                    StorageMap priceList = new StorageMap(Storage.CurrentContext, key);
                    priceMap[key] = priceList.Get(symbol);
                }
            }
            return priceMap;
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

        private static ProviderStatus ByteString2ProviderStatus(ByteString value)
        {
            if (value == null || value.Length == 0) return ProviderStatus.NotRegistered;
            return (ProviderStatus)(int)(BigInteger)value;
        }

        public static int test()
        {
            return 10;
        }
    }
}