using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;

namespace PriceFeedServicePublic
{
    [ManifestExtra("Description", "OKex Provider")]

    public class OKexProviderPublic : SmartContract
    {
        public const string Prefix_Price_URL = "https://www.okex.com/api/v5/market/mark-price-candles?bar=1m&limit=1";
        public const string Prefix_Price_InstId = "&instId=";
        public const string Prefix_Price_Time = "&after=";
        // { "code": "0", "msg": "", "data": [["1597026360000","11983.2","11988.5","11980.2","11988.2","26.43284234","316742.81553508"]]}
        public const string filter = "$.data[0][4]";
        public const long gasForResponse = Oracle.MinimumResponseFee; // TBD

        private const byte Prefix_Symbol = 0x02;
        private const byte Prefix_Block = 0x03;
        private const byte Prefix_Price = 0x04;

        [InitialValue("NUjRiSuVGYZAD9gJWFGn4tmCBHfyoWfrTF", ContractParameterType.Hash160)]
        private static readonly UInt160 Owner = default; //  Replace it with your own address

        private static StorageMap Symbols => new(Storage.CurrentContext, Prefix_Symbol);

        public static void _deploy(object data, bool update)
        {
            if (update) return;
            Symbols.Put("GAS-USDT", 0);
            Storage.Put(Storage.CurrentContext, new byte[] { Prefix_Block }, 0);
        }

        public static uint GetPriceRequest(string symbol) // "BTC-USDT"
        {
            uint blockIndex = Ledger.CurrentIndex;
            ulong timestamp = Ledger.GetBlock(blockIndex).Timestamp;
            string symbolUrl = Prefix_Price_URL + Prefix_Price_InstId + symbol + Prefix_Price_Time + timestamp;
            Oracle.Request(symbolUrl, filter, "getPriceCallback", blockIndex + "#" + symbol, 100000000);
            return blockIndex;
        }

        public static void GetPriceCallback(string url, string userdata, OracleResponseCode code, string result)
        {
            if (Runtime.CallingScriptHash != Oracle.Hash) throw new Exception("No authorization");
            if (code != OracleResponseCode.Success) throw new Exception("Oracle response failure with code " + (byte)code);

            object[] arr = (object[])StdLib.JsonDeserialize(result); // ["11988.2"]
            string value = (string)arr[0];  // string currentPrice
            string[] data = StdLib.StringSplit(userdata, "#");  // string blockIndex, string symbol
            UpdatePrice(data[0], data[1], value);
        }

        private static void UpdatePrice(string blockIndex, string symbol, string currentPrice)
        {
            if (Symbols[symbol] is null) throw new Exception("Symbol has not registered.");
            Storage.Put(Storage.CurrentContext, new byte[] { Prefix_Block }, blockIndex);
            byte[] key = Helper.Concat((byte[])GetKey(symbol), (byte[])GetKey(blockIndex));
            StorageMap priceList = new(Storage.CurrentContext, Prefix_Price);
            priceList.Put(key, currentPrice);
        }

        public static string GetPrice(string symbol, string blockIndex)
        {
            if (Symbols[symbol] is null) throw new Exception($"Symbol has not registered.");
            byte[] recordKey = Helper.Concat((byte[])GetKey(symbol), (byte[])GetKey(blockIndex));
            StorageMap priceList = new(Storage.CurrentContext, Prefix_Price);
            if (priceList[recordKey] is null) throw new Exception("The price of the symbol and blockIndex does not exist");
            return priceList[recordKey];
        }
        public static string GetPrice(string symbol)
        {
            if (Symbols[symbol] is null) throw new Exception($"Symbol has not registered.");
            byte[] recordKey = Helper.Concat((byte[])GetKey(symbol), (byte[])GetKey(BestBlockIndex()));
            StorageMap priceList = new(Storage.CurrentContext, Prefix_Price);
            if (priceList[recordKey] is null) throw new Exception("The price of the symbol and blockIndex does not exist");
            return priceList[recordKey];
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

        private static ByteString GetKey(string data)
        {
            return CryptoLib.ripemd160(data);
        }

        private static bool IsOwner() => Runtime.CheckWitness(Owner);

        public static string BestBlockIndex() => Storage.Get(Storage.CurrentContext, new byte[] { Prefix_Block });

        public static bool AddSymbol(string symbol)
        {
            if (!IsOwner()) throw new Exception("No authorization");
            if (Symbols[symbol] is not null) throw new InvalidOperationException("The symbol already exists.");
            Symbols.Put(symbol, 0);
            return true;
        }

        public static bool RemoveSymbol(string symbol)
        {
            if (!IsOwner()) throw new Exception("No authorization");
            if (Symbols[symbol] is null) throw new InvalidOperationException("The symbol does not exist.");
            Symbols.Delete(symbol);
            return true;
        }

        public static string[] GetAvailableSymbols()
        {
            List<string> ret = new();
            Iterator symbolList = Symbols.Find(FindOptions.RemovePrefix | FindOptions.KeysOnly);
            while (symbolList.Next())
            {
                ret.Add((string)symbolList.Value);
            }
            return ret;
        }

    }
}
