using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;

namespace PriceFeedService
{
    public class OKexProvider : SmartContract
    {
        public const string Prefix_Price_URL = "https://www.okex.com/api/v5/market/history-candles?bar=1m&limit=1";
        public const string Prefix_Price_InstId = "&instId=";
        public const string Prefix_Price_Time = "&after=";
        // { "code": "0", "msg": "", "data": [["1597026360000","11983.2","11988.5","11980.2","11988.2","26.43284234","316742.81553508"]]}
        public const string filter = "$.data[0][4]";
        public const long gasForResponse = Oracle.MinimumResponseFee; // TBD
        private static StorageMap price => Storage.CurrentContext.CreateMap(nameof(price));
        private static StorageMap tradingPair => Storage.CurrentContext.CreateMap(nameof(tradingPair));
        private static readonly UInt160 Owner = "NLq7pkzkWi1eZLi1thgm36KbGg6HYTM8Jv".ToScriptHash(); // Changed

        public static string Name => "OKexProvider";

        public static void GetLatestPriceRequest(string symbol) // BTC-USDT, 1597026383085
        {
            ulong blockTime = Ledger.GetBlock(Ledger.CurrentIndex).Timestamp;
            string symbolUrl = Prefix_Price_URL + Prefix_Price_InstId + symbol + Prefix_Price_Time + blockTime;
            Oracle.Request(symbolUrl, filter, "getLatestPriceCallback", symbol, 100000000);
        }

        public static void GetLatestPriceCallback(string url, string userdata, OracleResponseCode code, string result)
        {
            if (ExecutionEngine.CallingScriptHash != Oracle.Hash) throw new Exception("Unauthorized!");
            if (code != OracleResponseCode.Success) throw new Exception("Oracle response failure with code " + (byte)code);

            object[] arr = (object[])StdLib.JsonDeserialize(result); // ["11988.2"]
            string value = (string)arr[0];
            price.Put(userdata, value);
        }

        public static string GetLatestPrice(string symbol)
        {
            string latestPrice = price.Get(nameof(price) + symbol);
            if (latestPrice == null) throw new Exception("asset price does not exist");
            return latestPrice;
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
    }
}
