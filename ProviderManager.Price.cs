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
            if (!IsOwner()) throw new Exception("No authorization");
            if (Symbols[symbol] is null) throw new Exception("Symbol has not registered.");
            UInt160[] registeredProviders = GetRegisteredProviders();
            uint currentIndex = Ledger.CurrentIndex;
            foreach (UInt160 provider in registeredProviders)
            {
                Contract.Call(provider, "getPriceRequest", CallFlags.All, new object[] { currentIndex, symbol });
            }
            return currentIndex;
        }

        public static void UpdatePriceByProvider(string blockIndex, string symbol, string currentPrice)
        {
            if (Symbols[symbol] is null) throw new Exception("Symbol has not registered.");
            UInt160 provider = Runtime.CallingScriptHash;
            if (Providers[provider] is null) throw new Exception("No such provider registered");
            StorageMap priceList = new(Storage.CurrentContext, provider);
            PriceState state = new PriceState
            {
                CurrentPrice = currentPrice,
                Expiration = Runtime.Time + OneYear
            };
            byte[] key = Helper.Concat((byte[])GetKey(symbol), (byte[])GetKey(blockIndex));
            Storage.Put(Storage.CurrentContext, new byte[] { Prefix_Block }, blockIndex);
            priceList.Put(key, StdLib.Serialize(state));
        }

        public static object GetPrice(string symbol, string blockIndex, UInt160[] requiredProviders = null)
        {
            if (Symbols[symbol] is null) throw new Exception($"Symbol has not registered.");
            Map<UInt160, string> priceMap = new();
            byte[] recordKey = Helper.Concat((byte[])GetKey(symbol), (byte[])GetKey(blockIndex));
            if (Providers != null && requiredProviders.Length > 0)
            {
                foreach (UInt160 key in requiredProviders)
                {
                    if (Providers[key] is null) throw new Exception($"Provider has not registered.");
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
            if (priceList[recordKey] is null) throw new Exception("The price of the symbol and blockIndex does not exist");
            PriceState price = (PriceState)StdLib.Deserialize(priceList[recordKey]);
            price.EnsureNotExpired();
            priceMap[provider] = price.CurrentPrice;
        }
    }
}
