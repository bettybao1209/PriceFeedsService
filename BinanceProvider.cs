using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;

namespace PriceFeedService
{
    public class BinanceProvider : SmartContract
    {
        public const string Prefix_Price_URL = "https://binance.com/api/v3/klines?interval=1m&limit=1";
        public const string Prefix_Price_InstId = "&symbol=";  // NEOUSDT
        public const string Prefix_Price_Time = "&endTime=";
        // [[1511149320000,"36.00000000","36.00000000","36.00000000","36.00000000","1.14000000",1511149379999,"41.04000000",2,
        // "0.57000000", "20.52000000", "3039601.46000000"]]
        public const string filter = "$[4]";
        public const long gasForResponse = Oracle.MinimumResponseFee; // TBD
        private static StorageMap price => Storage.CurrentContext.CreateMap(nameof(price));
        private static StorageMap tradingPair => Storage.CurrentContext.CreateMap(nameof(tradingPair));

        public static void GetLatestPriceRequest(string symbol) // BTC-USDT, 1597026383085
        {
            ulong blockTime = Ledger.GetBlock(Ledger.CurrentIndex).Timestamp;
            string symbolUrl = Prefix_Price_URL + Prefix_Price_InstId + symbol + Prefix_Price_Time + blockTime;
            Oracle.Request(symbolUrl, null, "getLatestPriceCallback", symbol, 100000000);
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
    }
}
