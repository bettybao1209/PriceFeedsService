using Neo.SmartContract.Framework.Services;
using System;

namespace PriceFeedService
{
    public class PriceState
    {
        public string CurrentPrice;
        public ulong Expiration;

        public void EnsureNotExpired()
        {
            if (Runtime.Time >= Expiration)
                throw new Exception("The name has expired.");
        }
    }
}
