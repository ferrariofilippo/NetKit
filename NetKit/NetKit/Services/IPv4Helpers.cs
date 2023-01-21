using NetKit.Model;
using System.Collections.Generic;

namespace NetKit.Services
{
	public static class IPv4Helpers
	{
		private static readonly byte[] evenOddWildcardMask = new byte[4] { 0xFF, 0xFF, 0xFF, 0xFE };

        public static readonly List<uint> SubnetMaxHosts = new List<uint>();

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
						mask[j] |= (byte)MathHelpers.PowerOfTwo(7 - k);
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
			while (index < SubnetMaxHosts.Count && hosts > SubnetMaxHosts[index++]) ;
			if (index >= SubnetMaxHosts.Count)
				return 0;

			return SubnetMaxHosts[--index];
		}

		public static byte GetPrefixLength(uint hosts)
		{
			byte prefix = 0;
			while (hosts > SubnetMaxHosts[prefix++]) ;
			return (byte)(31 - prefix); 
		}

        public static ACE CalculateEvenOrOddWildcard(bool isEven, byte[] networkAddress)
        {
			for (int i = 0; i < evenOddWildcardMask.Length; i++)
				networkAddress[i] &= evenOddWildcardMask[i];
			if (!isEven)
				networkAddress[3]++;
			return new ACE
			{
				SupportAddress = $"{networkAddress[0]}.{networkAddress[1]}.{networkAddress[2]}.{networkAddress[3]}",
				WildcardMask = $"{evenOddWildcardMask[0]}.{networkAddress[1]}.{networkAddress[2]}.{networkAddress[3]}"
			};
        }

		private static void GetSubnetMaxHosts()
		{
			SubnetMaxHosts.Clear();
			SubnetMaxHosts.Add(4);
			for (int i = 0; i < 30; i++)
				SubnetMaxHosts.Add(SubnetMaxHosts[i] << 1);
			for (int i = 0; i < SubnetMaxHosts.Count; i++)
				SubnetMaxHosts[i] -= 2;
		}
    }
}
