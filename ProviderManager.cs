using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;
using System.Numerics;

namespace PriceFeedService
{
    [ManifestExtra("Description", "Neo PriceFeeds Provider Manager")]
    [ContractPermission("*", "getPriceRequest")]
    public partial class ProviderManager : SmartContract
    {
        private const byte Prefix_Providers = 0x01;
        private const byte Prefix_Symbol = 0x02;
        private static StorageMap Providers => new(Storage.CurrentContext, Prefix_Providers);
        private static StorageMap Symbols => new(Storage.CurrentContext, Prefix_Symbol);

        private const uint OneYear = 365 * 24 * 3600;
        [InitialValue("NWhJATyChXvaBqS9debbk47Uf2X33WtHtL", ContractParameterType.Hash160)]
        private static readonly UInt160 Owner = default; //  Replace it with your own address

        public static void _deploy(object data, bool update)
        {
            if (update) return;
            Symbols.Put("BTC-USDT", 0);
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
