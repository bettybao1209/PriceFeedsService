using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.Numerics;

namespace PriceFeedService
{
    public enum ProviderStatus
    {
        NotRegistered = 0,
        Registered = 1
    }

    public class ProviderRegister : SmartContract
    {
        private static StorageMap providers => Storage.CurrentContext.CreateMap(nameof(providers));
        private static StorageMap price => Storage.CurrentContext.CreateMap(nameof(price));
        private static readonly UInt160 Owner = "NLq7pkzkWi1eZLi1thgm36KbGg6HYTM8Jv".ToScriptHash(); // Changed


        public static bool RegisterProvider(UInt160 provider)
        {
            if (!Runtime.CheckWitness(Owner)) throw new Exception("Unauthorized!");
            ProviderStatus status = ByteArray2ProviderStatus((byte[])providers.Get(provider));
            if (status == ProviderStatus.Registered) throw new Exception("Provider already registered");
            byte[] registered = IssuerStatus2ByteArray(ProviderStatus.Registered);
            providers.Put(provider, (ByteString)registered);
            return true;
        }

        public static UInt160[] GetAvailableProviders()
        {
            List<UInt160> ret = new List<UInt160>();
            Iterator providersList = Storage.Find(Storage.CurrentContext, nameof(providers), FindOptions.KeysOnly);
            while (providersList.Next())
            {
                ret.Add((UInt160)providersList.Value);
            }
            return ret;
        }

        public static bool UnRegisterProvider(UInt160 provider)
        {
            if (!Runtime.CheckWitness(Owner)) throw new Exception("Unauthorized!");
            ProviderStatus status = ByteArray2ProviderStatus((byte[])providers.Get(provider));
            if (status == ProviderStatus.NotRegistered) throw new Exception("No such provider registered");
            providers.Delete(provider);
            return true;
        }

        public static void UpdatePrice(UInt160 provider, string currentPrice)
        {
            if (ExecutionEngine.CallingScriptHash != provider) throw new Exception("Unautherized!");
            ProviderStatus status = ByteArray2ProviderStatus((byte[])providers.Get(provider));
            if (status == ProviderStatus.NotRegistered) throw new Exception("No such provider registered");
            price.Put(provider, currentPrice);
        }

        private static byte[] IssuerStatus2ByteArray(ProviderStatus value) => ((BigInteger)(int)value).ToByteArray();

        private static ProviderStatus ByteArray2ProviderStatus(byte[] value)
        {
            if (value == null || value.Length == 0) return ProviderStatus.NotRegistered;
            return (ProviderStatus)(int)value.ToBigInteger();
        }
    }
}
