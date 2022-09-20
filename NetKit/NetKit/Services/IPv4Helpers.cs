using System;
using System.Collections.Generic;
using System.Text;

namespace NetKit.Services
{
	public static class IPv4Helpers
	{
		public static bool ValidateSubnetMask(byte[] outSubnet, string[] subnetComponents)
		{
			if (subnetComponents.Length != 4)
				return false;

			try
			{
				for (byte i = 0; i < subnetComponents.Length; i++)
					outSubnet[i] = byte.Parse(subnetComponents[i]);
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}

		public static void GetSubnetMaxHosts(List<uint> values)
		{
			values.Add(4);
			for (int i = 0; i < 30; i++)
				values.Add(values[i] << 1);
			for (int i = 0; i < values.Count; i++)
				values[i] -= 2;
		}

		public static byte[] GetSubnetMask(byte prefixLength)
		{
			byte[] mask = new byte[4];
			for (int j = 0; j < 4; j++)
			{
				for (int k = 0; k < 8; k++)
				{
					if (j * 8 + k < prefixLength)
						mask[j] += (byte)MathHelpers.PowerOfTwo(7 - k);
					else
						return mask;
				}
			}
			return mask;
		}
	}
}
