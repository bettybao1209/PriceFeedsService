using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;

namespace PriceFeedService
{
    partial class ProviderManager
    {
        public static uint TriggerCurrentPrice(string symbol)
        {
            UInt160[] registeredProviders = GetRegisteredProviders();
            foreach (UInt160 provider in registeredProviders)
            {
                Contract.Call(provider, "getPriceRequest", CallFlags.All, new object[] { Ledger.CurrentIndex, symbol });
            }
            return Ledger.CurrentIndex;
        }

        public static void UpdatePriceByProvider(string blockIndex, string symbol, string currentPrice)
        {
            UInt160 provider = Runtime.CallingScriptHash;
            ProviderStatus status = ByteString2ProviderStatus(Providers.Get(provider));
            if (status == ProviderStatus.NotRegistered) throw new Exception("No such provider registered");
            StorageMap priceList = new(Storage.CurrentContext, provider);
            PriceState state = new PriceState
            {
                CurrentPrice = currentPrice,
                Expiration = Runtime.Time + 10
            };
            byte[] key = Helper.Concat((byte[])GetKey(symbol), (byte[])GetKey(blockIndex));
            priceList.Put(key, StdLib.Serialize(state));
        }

        public static object GetPrice(string symbol, string blockIndex, UInt160[] requiredProviders = null)
        {
            Map<UInt160, string> priceMap = new();
            byte[] recordKey = Helper.Concat((byte[])GetKey(symbol), (byte[])GetKey(blockIndex));
            if (Providers != null && requiredProviders.Length > 0)
            {
                foreach (UInt160 key in requiredProviders)
                {
                    ProviderStatus status = ByteString2ProviderStatus(Providers.Get(key));
                    if (status == ProviderStatus.NotRegistered) throw new Exception($"Provider has not registered.");
                    GetPriceByProvider(key, recordKey, priceMap);
                }
            }
            else
            {
                UInt160[] registeredProvider = GetRegisteredProviders();
                foreach (UInt160 key in registeredProvider)
                {
                    GetPriceByProvider(key, recordKey, priceMap);
                }
            }
            return priceMap;
        }

        private static void GetPriceByProvider(UInt160 provider, byte[] recordKey, Map<UInt160, string> priceMap)
        {
            StorageMap priceList = new(Storage.CurrentContext, provider);
            PriceState price = (PriceState)StdLib.Deserialize(priceList[recordKey]);
            price.EnsureNotExpired();
            priceMap[provider] = price.CurrentPrice;
        }
    }
}
