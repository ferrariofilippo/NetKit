using NetKit.Model;
using System.Collections.Generic;

namespace NetKit.Services
{
    public class WildcardHelpers
    {
        private const int ADDRESS_BITS_INDEXER = 31;    // Starts from 0 (32 bits)
        private const int BITS_PER_BYTE = 8;
        private const int BYTES_PER_ADDRESS = 4;
        private const int ADDRESS_BITS = 32;

        private static readonly byte[] evenOddWildcardMask = new byte[4] { 0xFF, 0xFF, 0xFF, 0xFE };

        public static ACE CalculateEvenOrOddWildcard(bool isEven, byte[] networkAddress, int networkBits)
        {
            var mask = GetNetworkWildcard(networkBits);
            mask[0] &= evenOddWildcardMask[0];
            mask[1] &= evenOddWildcardMask[1];
            mask[2] &= evenOddWildcardMask[2];
            mask[3] &= evenOddWildcardMask[3];

            if (isEven)
                networkAddress[networkBits / BITS_PER_BYTE] &= 0xFE;
            else
                networkAddress[networkBits / BITS_PER_BYTE] |= 1;

            return new ACE
            {
                SupportAddress = $"{networkAddress[0]}.{networkAddress[1]}.{networkAddress[2]}.{networkAddress[3]}",
                WildcardMask = $"{mask[0]}.{mask[1]}.{mask[2]}.{mask[3]}"
            };
        }

        public static List<ACE> CalculateGreaterThanWildcardMask(byte[] networkAddress, uint lowerBound, int networkBits)
        {
            var exponent = MathHelpers.GetExcessBase2Log(lowerBound);
            if (exponent <= 0)
                return new List<ACE>();

            var upperBound = MathHelpers.PowersOfTwo[exponent];
            var approximatedLowerBound = lowerBound + (lowerBound % 2);
            var byteIndex = 3 - (exponent / BITS_PER_BYTE);
            var roundedExponent = exponent - (exponent % BITS_PER_BYTE);     // This way we get only 0, 8, 16, 24

            var wildcard = GetNetworkWildcard(networkBits);
            var aces = new List<ACE>()
            {
                GetBiggestEntry(exponent, wildcard, networkAddress)
            };

            while (upperBound > approximatedLowerBound)
                aces.Add(GetGreaterThanEntry(networkAddress, wildcard, lowerBound, ref upperBound, exponent));

            wildcard[byteIndex] = 0;
            while (--upperBound >= lowerBound)
            {
                networkAddress[byteIndex] = (byte)(upperBound >> roundedExponent);
                aces.Add(CreateACE(networkAddress, wildcard));
            }

            return aces;
        }

        public static List<ACE> CalculateSmallerThanWildcardMask(byte[] networkAddress, uint upperBound, int networkBits)
        {
            var exponent = MathHelpers.GetDefectBase2Log(upperBound);
            if (exponent <= 0)
                return new List<ACE>();

            var lowerBound = MathHelpers.PowersOfTwo[exponent];
            var approximatedUpperBound = upperBound - (upperBound % 2);
            var byteIndex = 3 - exponent / BITS_PER_BYTE;
            var roundedExponent = exponent - (exponent % BITS_PER_BYTE);     // This way we get only 0, 8, 16, 24

            var wildcard = GetNetworkWildcard(networkBits);
            var aces = new List<ACE>()
            {
                GetBiggestEntryForSmaller(exponent, wildcard, networkAddress)
            };

            while (lowerBound < approximatedUpperBound)
                aces.Add(GetSmallerThanEntry(networkAddress, wildcard, upperBound, ref lowerBound, exponent));

            wildcard[byteIndex] = 0;
            while (++lowerBound <= upperBound)
            {
                networkAddress[byteIndex] = (byte)(lowerBound >> roundedExponent);
                aces.Add(CreateACE(networkAddress, wildcard));
            }

            return aces;
        }

        public static List<ACE> CalculateRangeWildcardMask(byte[] networkAddress, uint lowerBound, uint upperBound, int networkBits)
        {
            var aces = new List<ACE>();
            if (lowerBound == upperBound)
            {
                int byteIndex = networkBits / BITS_PER_BYTE;
                networkAddress[byteIndex] = (byte)(lowerBound >> (byteIndex * BITS_PER_BYTE));
                aces.Add(CreateACE(
                    networkAddress,
                    new byte[BYTES_PER_ADDRESS] { 0, 0, 0, 0 }));
                return aces;
            }

            var wildcard = GetNetworkWildcard(networkBits);
            var lowerExponent = MathHelpers.GetExcessBase2Log(lowerBound);
            var upperExponent = MathHelpers.GetDefectBase2Log(upperBound);

            var medianPowerOfTwo = MathHelpers.PowersOfTwo[lowerExponent];
            if (upperExponent > lowerExponent + 1)
            {
                while (upperBound > medianPowerOfTwo)
                    aces.Add(GetGreaterThanEntry(networkAddress, wildcard, medianPowerOfTwo, ref upperBound, upperExponent));
                while (lowerBound < medianPowerOfTwo)
                    aces.Add(GetSmallerThanEntry(networkAddress, wildcard, medianPowerOfTwo, ref lowerBound, lowerExponent));
            }
            else
            {
                // TODO: Optimize this (It works but it doesn't merge ACEs when it could)
                while (lowerBound < upperBound)
                    aces.Add(GetSmallerThanEntry(networkAddress, wildcard, upperBound, ref lowerBound, lowerExponent));
            }

            return aces;
        }

        private static byte[] GetNetworkWildcard(int networkBits)
        {
            var mask = 0u;
            for (int i = 0; i < ADDRESS_BITS; i++)
            {
                if (i >= networkBits)
                    mask++;
                if (i != ADDRESS_BITS_INDEXER)
                    mask <<= 1;
            }

            return new byte[BYTES_PER_ADDRESS]
            {
                (byte)(mask >> 24),
                (byte)(mask >> 16 & 0x00FF),
                (byte)(mask >> 8 & 0x0000FF),
                (byte)(mask & 0x000000FF)
            };
        }

        private static ACE GetBiggestEntry(int exponent, byte[] mask, byte[] networkAddress)
        {
            var byteIndex = 3 - exponent / BITS_PER_BYTE;
            var bitIndex = exponent % BITS_PER_BYTE;
            var tempMask = new byte[BYTES_PER_ADDRESS] { mask[0], mask[1], mask[2], mask[3] };

            tempMask[byteIndex] &= (byte)(~MathHelpers.PowersOfTwo[bitIndex]);
            networkAddress[byteIndex] = (byte)MathHelpers.PowersOfTwo[bitIndex];

            return CreateACE(networkAddress, tempMask);
        }

        private static ACE GetBiggestEntryForSmaller(int exponent, byte[] mask, byte[] networkAddress)
        {
            var byteIndex = 3 - exponent / BITS_PER_BYTE;
            var bitIndex = exponent % BITS_PER_BYTE;
            var tempMask = new byte[BYTES_PER_ADDRESS] { mask[0], mask[1], mask[2], mask[3] };

            tempMask[byteIndex] &= (byte)(0xFF >> (BITS_PER_BYTE - bitIndex));
            networkAddress[byteIndex] = 0;

            return CreateACE(networkAddress, tempMask);
        }

        private static ACE GetGreaterThanEntry(byte[] networkAddress, byte[] mask, uint lowerBound, ref uint upperBound, int exponent)
        {
            var byteIndex = 3 - exponent / BITS_PER_BYTE;
            var approxExponent = exponent - (exponent % BITS_PER_BYTE);   // This way we get only 0, 8, 16, 24
            var addressesSum = 0u;
            var settedBits = 0u;
            for (int i = ADDRESS_BITS_INDEXER; i >= exponent; i--)
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

            var tempMask = new byte[BYTES_PER_ADDRESS] { mask[0], mask[1], mask[2], mask[3] };
            var unsetBits = (byte)(~settedBits >> approxExponent);

            tempMask[byteIndex] &= unsetBits;
            networkAddress[byteIndex] = (byte)(addressesSum >> approxExponent);
            upperBound = addressesSum;

            return CreateACE(networkAddress, tempMask);
        }

        private static ACE GetSmallerThanEntry(byte[] networkAddress, byte[] mask, uint upperBound, ref uint lowerBound, int exponent)
        {
            var byteIndex = 3 - exponent / BITS_PER_BYTE;
            var approxExponent = exponent - (exponent % BITS_PER_BYTE);   // This way we get only 0, 8, 16, 24
            var addressesSum = lowerBound;
            var settedBits = 0u;
            exponent--;
            for (int i = ADDRESS_BITS_INDEXER; i >= exponent; i--)
                settedBits += MathHelpers.PowersOfTwo[i];

            while (exponent >= 0 && (addressesSum + MathHelpers.PowersOfTwo[exponent]) > upperBound)
            {
                exponent--;
                settedBits += MathHelpers.PowersOfTwo[exponent];
            }

            var tempMask = new byte[BYTES_PER_ADDRESS] { mask[0], mask[1], mask[2], mask[3] };
            var unsetBits = (byte)(~settedBits >> approxExponent);

            tempMask[byteIndex] &= unsetBits;
            networkAddress[byteIndex] = (byte)(addressesSum >> approxExponent);
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
