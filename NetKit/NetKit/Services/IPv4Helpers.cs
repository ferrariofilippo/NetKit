using NetKit.Model;
using System.Collections.Generic;
using Xamarin.Forms;

namespace NetKit.Services
{
    public static class IPv4Helpers
    {
        private static readonly byte[] evenOddWildcardMask = new byte[4] { 0xFF, 0xFF, 0xFF, 0xFE };

        public static readonly uint[] SubnetMaxHosts = new uint[31];

        public static void Init()
        {
            GetSubnetMaxHosts();
        }

        public static bool TryGetSubnetMask(byte prefixLength, byte[] mask)
        {
            if (mask.Length != 4)
                return false;
            for (int j = 0; j < 4; j++)
            {
                for (int k = 0; k < 8; k++)
                {
                    if (j * 8 + k < prefixLength)
                        mask[j] |= (byte)MathHelpers.PowersOfTwo[7 - k];
                    else
                        return true;
                }
            }
            return true;
        }

        public static bool TryParseAddress(string addressString, byte[] address)
        {
            if (string.IsNullOrWhiteSpace(addressString) || address.Length != 4)
                return false;

            string[] addressComponents = addressString.Split('.');
            if (addressComponents.Length != 4)
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
            return (byte)(31 - prefix);
        }

        public static ACE CalculateEvenOrOddWildcard(bool isEven, byte[] networkAddress, int networkBits)
        {
            var mask = GetNetworkWildcard(networkBits);
            mask[0] &= evenOddWildcardMask[0];
            mask[1] &= evenOddWildcardMask[1];
            mask[2] &= evenOddWildcardMask[2];
            mask[3] &= evenOddWildcardMask[3];

            if (isEven)
                networkAddress[networkBits / 8] &= 0xFE;
            else
                networkAddress[networkBits / 8] |= 1;

            return new ACE
            {
                SupportAddress = $"{networkAddress[0]}.{networkAddress[1]}.{networkAddress[2]}.{networkAddress[3]}",
                WildcardMask = $"{mask[0]}.{mask[1]}.{mask[2]}.{mask[3]}"
            };
        }

        public static List<ACE> CalculateGreaterThanWildcardMask(byte[] networkAddress, uint lowerBound, int networkBits)
        {
            var exponent = MathHelpers.GetExcessBase2Log(lowerBound);
            var wildcard = GetNetworkWildcard(networkBits);
            var aces = new List<ACE>()
            {
                GetBiggestEntry(exponent, wildcard, networkAddress)
            };

            var upperBound = MathHelpers.PowersOfTwo[exponent];
            var isLowerBoundOdd = lowerBound % 2;

            while (upperBound > (lowerBound + isLowerBoundOdd))
                aces.Add(GetGreaterThanEntry(networkAddress, wildcard, lowerBound, ref upperBound, exponent));

            var whichByte = 3 - exponent / 8;
            var roundedExponent = (3 - whichByte) * 8;     // This way we get only 0, 8, 16, 24
            while (--upperBound >= lowerBound)
            {
                networkAddress[whichByte] = (byte)(upperBound >> roundedExponent);
                wildcard[whichByte] = 0;
                aces.Add(CreateACE(networkAddress, wildcard));
            }

            return aces;
        }

        public static List<ACE> CalculateSmallerThanWildcardMask(byte[] networkAddress, uint upperBound, int networkBits)
        {
            // TOOD: Optimize. ATM this creates 2 ACEs that can be merged
            var exponent = MathHelpers.GetDefectBase2Log(upperBound);
            var wildcard = GetNetworkWildcard(networkBits);
            var aces = new List<ACE>()
            {
                GetBiggestEntryForSmaller(exponent, wildcard, networkAddress)
            };

            var lowerBound = MathHelpers.PowersOfTwo[exponent];
            var isUpperBoundOdd = upperBound % 2;

            while (lowerBound < (upperBound - isUpperBoundOdd))
                aces.Add(GetSmallerThanEntry(networkAddress, wildcard, upperBound, ref lowerBound, exponent));

            var whichByte = 3 - exponent / 8;
            var roundedExponent = (3 - whichByte) * 8;     // This way we get only 0, 8, 16, 24
            while (++lowerBound <= upperBound)
            {
                networkAddress[whichByte] = (byte)(lowerBound >> roundedExponent);
                wildcard[whichByte] = 0;
                aces.Add(CreateACE(networkAddress, wildcard));
            }

            return aces;
        }

        public static List<ACE> CalculateRangeWildcardMask(byte[] networkAddress, uint loweUpperBound, uint upperBound, int networkBits)
        {
            var wildcard = GetNetworkWildcard(networkBits);
            var aces = new List<ACE>();

            // TODO

            return aces;
        }

        private static void GetSubnetMaxHosts()
        {
            SubnetMaxHosts[0] = 4;
            for (int i = 0; i < 30; i++)
                SubnetMaxHosts[i + 1] = (SubnetMaxHosts[i] << 1);
            for (int i = 0; i < SubnetMaxHosts.Length; i++)
                SubnetMaxHosts[i] -= 2;
        }

        private static byte[] GetNetworkWildcard(int networkBits) 
        {
            var mask = 0u;
            for (int i = 0; i < 32; i++)
            {
                if (i >= networkBits)
                    mask++;
                if (i != 31)
                    mask <<= 1;
            }

            return new byte[4]
            {
                (byte)(mask >> 24),
                (byte)(mask >> 16 & 0x00FF),
                (byte)(mask >> 8 & 0x0000FF),
                (byte)(mask & 0x000000FF)
            };
        }

        private static ACE GetBiggestEntry(int exponent, byte[] mask, byte[] networkAddress)
        {
            var whichByte = 3 - exponent / 8;
            var whichBit = exponent % 8;

            var tempMask = new byte[4] { mask[0], mask[1], mask[2], mask[3] };
            tempMask[whichByte] ^= (byte)MathHelpers.PowersOfTwo[whichBit];
            networkAddress[whichByte] = (byte)MathHelpers.PowersOfTwo[whichBit];

            return CreateACE(networkAddress, tempMask);
        }

        private static ACE GetBiggestEntryForSmaller(int exponent, byte[] mask, byte[] networkAddress)
        {
            var whichByte = 3 - exponent / 8;
            var whichBit = exponent % 8;

            var tempMask = new byte[4] { mask[0], mask[1], mask[2], mask[3] };
            tempMask[whichByte] ^= (byte)(~(0xFF >> (8 - whichBit)));
            networkAddress[whichByte] = 0;

            return CreateACE(networkAddress, tempMask);
        }

        private static ACE GetGreaterThanEntry(byte[] networkAddress, byte[] mask, uint lowerBound, ref uint upperBound, int exponent)
        {
            var whichByte = 3 - exponent / 8;
            var exponentCopy = (3 - whichByte) * 8;   // This way we get only 0, 8, 16, 24
            var addressesSum = 0u;
            var settedBits = 0u;
            for (int i = 31; i >= exponent; i--)
                settedBits += MathHelpers.PowersOfTwo[i];
            exponent--;

            while (addressesSum < lowerBound && exponent >= 0)
            {
                addressesSum += MathHelpers.PowersOfTwo[exponent];
                settedBits += MathHelpers.PowersOfTwo[exponent];
                if (addressesSum >= upperBound)
                    addressesSum -= MathHelpers.PowersOfTwo[exponent];
                exponent--;
            }

            var tempMask = new byte[4] { mask[0], mask[1], mask[2], mask[3] };
            var unsetBits = (byte)(~settedBits >> exponentCopy);

            tempMask[whichByte] &= unsetBits;
            networkAddress[whichByte] = (byte)(addressesSum >> exponentCopy);
            upperBound = addressesSum;

            return CreateACE(networkAddress, tempMask);
        }

        private static ACE GetSmallerThanEntry(byte[] networkAddress, byte[] mask, uint upperBound, ref uint lowerBound, int exponent)
        {
            var whichByte = 3 - exponent / 8;
            var exponentCopy = (3 - whichByte) * 8;   // This way we get only 0, 8, 16, 24
            var addressesSum = lowerBound;
            var settedBits = 0u;
            for (int i = 31; i >= exponent; i--)
                settedBits += MathHelpers.PowersOfTwo[i];
            exponent--;

            while (exponent >= 0 && (addressesSum + MathHelpers.PowersOfTwo[exponent]) > upperBound)
            {
                settedBits += MathHelpers.PowersOfTwo[exponent];
                exponent--;
            }

            var tempMask = new byte[4] { mask[0], mask[1], mask[2], mask[3] };
            var unsetBits = (byte)(~settedBits >> exponentCopy);

            tempMask[whichByte] &= unsetBits;
            networkAddress[whichByte] = (byte)(addressesSum >> exponentCopy);
            lowerBound = addressesSum + MathHelpers.PowersOfTwo[exponent];

            return CreateACE(networkAddress, tempMask);
        }

        private static ACE CreateACE(byte[] networkAddress, byte[] wildcard)
        {
            return new ACE
            {
                SupportAddress = $"{networkAddress[0]}.{networkAddress[1]}.{networkAddress[2]}.{networkAddress[3]}",
                WildcardMask = $"{wildcard[0]}.{wildcard[1]}.{wildcard[2]}.{wildcard[3]}"
            };
        }
    }
}
