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

    public class PriceState
    {
        public string symbol;
        public uint index;
    }

    [ContractPermission("0xfffdc93764dbaddd97c48f252a53ea4643faa3fd", "destroy", "update")]
    [ContractPermission("*", "getPriceRequest")]
    public partial class ProviderManager : SmartContract
    {
        private const byte Prefix_Providers = 0x01;
        private static StorageMap providers => new StorageMap(Storage.CurrentContext, Prefix_Providers);

        [InitialValue("NWhJATyChXvaBqS9debbk47Uf2X33WtHtL", ContractParameterType.Hash160)]
        private static readonly UInt160 Owner = default; //  Replace it with your own address

        public static uint TriggerCurrentPrice(string symbol)
        {
            UInt160[] registeredProviders = GetRegisteredProviders();
            foreach (UInt160 provider in registeredProviders)
            {
                Contract.Call(provider, "getPriceRequest", CallFlags.All, new object[] { Ledger.CurrentIndex, symbol });
            }
            return Ledger.CurrentIndex;
        }

        public static void UpdatePriceByProvider(uint blockIndex, string symbol, string currentPrice)
        {
            UInt160 provider = Runtime.CallingScriptHash;
            ProviderStatus status = ByteString2ProviderStatus(providers.Get(provider));
            if (status == ProviderStatus.NotRegistered) throw new Exception("No such provider registered");
            StorageMap priceList = new StorageMap(Storage.CurrentContext, provider);
            PriceState state = new PriceState { symbol = symbol, index = blockIndex };
            priceList.Put(StdLib.Serialize(state), currentPrice);
        }

        public static object GetPrice(string symbol, uint blockIndex, UInt160[] requiredProviders = null)
        {
            Map<UInt160, string> priceMap = new Map<UInt160, string>();
            PriceState state = new PriceState { symbol = symbol, index = blockIndex };
            if (providers != null && requiredProviders.Length > 0)
            {
                foreach (UInt160 key in requiredProviders)
                {
                    ProviderStatus status = ByteString2ProviderStatus(providers.Get(key));
                    if (status == ProviderStatus.Registered)
                    {
                        StorageMap priceList = new StorageMap(Storage.CurrentContext, key);
                        priceMap[key] = priceList.Get(StdLib.Serialize(state));
                    }
                }
            }
            else
            {
                UInt160[] registeredProvider = GetRegisteredProviders();
                foreach (UInt160 key in registeredProvider)
                {
                    StorageMap priceList = new StorageMap(Storage.CurrentContext, key);
                    priceMap[key] = priceList.Get(StdLib.Serialize(state));
                }
            }
            return priceMap;
        }

        public static bool RegisterProvider(UInt160 provider)
        {
            if (!Runtime.CheckWitness(Owner)) throw new Exception("No authorization");
            ProviderStatus status = ByteString2ProviderStatus(providers.Get(provider));
            if (status == ProviderStatus.Registered) throw new Exception("Provider already registered");
            byte[] registered = IssuerStatus2ByteArray(ProviderStatus.Registered);
            providers.Put(provider, (ByteString)registered);
            return true;
        }

        public static bool UnRegisterProvider(UInt160 provider)
        {
            if (!Runtime.CheckWitness(Owner)) throw new Exception("No authorization");
            ProviderStatus status = ByteString2ProviderStatus(providers.Get(provider));
            if (status == ProviderStatus.NotRegistered) throw new Exception("No such provider registered");
            providers.Delete(provider);
            return true;
        }

        public static UInt160[] GetRegisteredProviders()
        {
            List<UInt160> ret = new List<UInt160>();
            Iterator providersList = Storage.Find(Storage.CurrentContext, new byte[] { Prefix_Providers }, FindOptions.RemovePrefix | FindOptions.KeysOnly);
            while (providersList.Next())
            {
                ret.Add((UInt160)providersList.Value);
            }
            return ret;

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
    }
}
