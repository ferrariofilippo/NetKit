namespace NetKit.Services
{
    public static class IPv4Helpers
    {
        private const int ADDRESS_BITS_INDEXER = 31;    // Starts from 0 (32 bits)
        private const int BITS_PER_BYTE = 8;
        private const int BITS_PER_BYTE_INDEXER = 7;    // Start from 0 (8 bits)
        private const int BYTES_PER_ADDRESS = 4;

        public static readonly uint[] SubnetMaxHosts = new uint[31];

        public static void Init()
        {
            GetSubnetMaxHosts();
        }

        public static bool TryGetSubnetMask(byte prefixLength, byte[] mask)
        {
            if (mask.Length != BYTES_PER_ADDRESS)
                return false;
            for (int j = 0; j < BYTES_PER_ADDRESS; j++)
            {
                for (int k = 0; k < BITS_PER_BYTE; k++)
                {
                    if (j * BITS_PER_BYTE + k < prefixLength)
                        mask[j] |= (byte)MathHelpers.PowersOfTwo[BITS_PER_BYTE_INDEXER - k];
                    else
                        return true;
                }
            }
            return true;
        }

        public static bool TryParseAddress(string addressString, byte[] address)
        {
            if (string.IsNullOrWhiteSpace(addressString) || address.Length != BYTES_PER_ADDRESS)
                return false;

            string[] addressComponents = addressString.Split('.');
            if (addressComponents.Length != BYTES_PER_ADDRESS)
                return false;

            for (byte i = 0; i < address.Length; i++)
            {
                if (!byte.TryParse(addressComponents[i], out address[i]))
                    return false;
            }

            return true;
        }

        public static uint GetMinimumWasteHostSize(byte prefixLength)
        {
            sbyte howManyCycles = (sbyte)(30 - prefixLength);
            if (howManyCycles >= 0)
                return SubnetMaxHosts[howManyCycles];
            return 0;
        }


        public static uint GetMinimumWasteHostSize(uint hosts)
        {
            byte index = 0;
            while (index < SubnetMaxHosts.Length && hosts > SubnetMaxHosts[index++]) ;
            if (index >= SubnetMaxHosts.Length)
                return 0;

            return SubnetMaxHosts[--index];
        }

        public static byte GetPrefixLength(uint hosts)
        {
            byte prefix = 0;
            while (hosts > SubnetMaxHosts[prefix++]) ;
            return (byte)(ADDRESS_BITS_INDEXER - prefix);
        }

        private static void GetSubnetMaxHosts()
        {
            SubnetMaxHosts[0] = 4;
            for (int i = 0; i < SubnetMaxHosts.Length - 1; i++)
                SubnetMaxHosts[i + 1] = (SubnetMaxHosts[i] << 1);
            for (int i = 0; i < SubnetMaxHosts.Length; i++)
                SubnetMaxHosts[i] -= 2;
        }
    }
}
