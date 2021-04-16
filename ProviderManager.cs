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

    [ManifestExtra("Description", "Neo Provider Manager")]
    [ContractPermission("0xfffdc93764dbaddd97c48f252a53ea4643faa3fd", "destroy", "update")]
    [ContractPermission("*", "getPriceRequest")]
    public partial class ProviderManager : SmartContract
    {
        private const byte Prefix_Providers = 0x01;
        private static StorageMap Providers => new(Storage.CurrentContext, Prefix_Providers);

        private const uint OneYear = 365 * 24 * 3600;
        [InitialValue("NWhJATyChXvaBqS9debbk47Uf2X33WtHtL", ContractParameterType.Hash160)]
        private static readonly UInt160 Owner = default; //  Replace it with your own address

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

        private static ByteString GetKey(string symbol)
        {
            return CryptoLib.ripemd160(symbol);
        }
    }
}
