using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using Neo.SmartContract.Framework.Attributes;

namespace PriceFeedService
{
    [ManifestExtra("Description", "PriceFeeds Provider Manager")]
    [ContractPermission("*", "getPriceRequest")]
    public partial class ProviderManager : SmartContract
    {
        private const byte Prefix_Providers = 0x01;
        private const byte Prefix_Symbol = 0x02;
        private const string Prefix_Block = "block";

        private const string Prefix_Price = "price";
        public static string BestBlockIndex() => Storage.Get(Storage.CurrentContext, Prefix_Block);
        private static StorageMap Providers => new(Storage.CurrentContext, Prefix_Providers);
        private static StorageMap Symbols => new(Storage.CurrentContext, Prefix_Symbol);

        private const ulong OneYear = 365ul * 24 * 3600 * 1000;
        [InitialValue("NWwhywny6GmyTHEFLxBpbaDNRuQE7fVL1g", ContractParameterType.Hash160)]
        private static readonly UInt160 Owner = default; //  Replace it with your own address

        public static void _deploy(object data, bool update)
        {
            if (update) return;
            //Symbols.Put("BTC-USDT", 0);
            //Symbols.Put("ETH-USDT", 0);
            //Symbols.Put("NEO-USDT", 0);
            Symbols.Put("GAS-USDT", 0);
            Storage.Put(Storage.CurrentContext, Prefix_Block , 0);
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

        private static ByteString GetKey(string data)
        {
            return CryptoLib.ripemd160(data);
        }
    }
}
