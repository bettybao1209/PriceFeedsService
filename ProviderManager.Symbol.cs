using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using System;

namespace PriceFeedService
{
    partial class ProviderManager
    {
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
            List<string> ret = null;
            Iterator symbolList = Symbols.Find(FindOptions.RemovePrefix | FindOptions.KeysOnly);
            while (symbolList.Next())
            {
                ret.Add((string)symbolList.Value);
            }
            return ret ?? new string[0];
        }
    }
}
