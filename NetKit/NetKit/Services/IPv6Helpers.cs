using System;
using System.Text;

namespace NetKit.Services
{
	public static class IPv6Helpers
	{
		private const int HEXTET_PER_ADDRESS = 8;
		private const int LAST_HEXTET_INDEX = 7;
		private const int UPPERCASE_A_ASCII = 65;
		private const int UPPERCASE_F_ASCII = 70;
		private const int COLON_ASCII = 58;
		private const int ZERO_ASCII = 48;
		private const int NINE_ACSII = 57;

		public static string Compress(ref string[] addressComponents, byte length)
		{
			var addressIsO = new bool[HEXTET_PER_ADDRESS];
			var parameters = new byte[2];
			var output = new StringBuilder();

			for (byte i = 0; i < length; i++)
				addressComponents[i] = string.IsNullOrWhiteSpace(addressComponents[i]) ? "0" : OmitLeadingOs(addressComponents[i], i, addressIsO);
			for (byte i = 0; i < length; i++)
			{
				byte j = i;
				byte size = 0;
				while (i < length && addressComponents[i++].Equals("0"))
					size++;
				if (size > parameters[1])
				{
					parameters[0] = j;
					parameters[1] = size;
				}
			}

			for (byte i = parameters[0]; i < parameters[1]; i++)
				addressComponents[i] = "";

			for (byte i = 0; i < length; i++)
			{
				if (addressComponents[i].Equals(""))
				{
					if (i == 0)
						output.Append("::");
					else
						output.Append(":");

					if (parameters[1] != 0)
						i += (byte)(parameters[1] - 1);
				}
				else
				{
					output.Append(addressComponents[i]);
					if (i != LAST_HEXTET_INDEX)
						output.Append(":");
				}
			}
			return output.ToString();
		}

		public static bool ValidateAddress(string address, out string[] addressComponents)
		{
			addressComponents = Array.Empty<string>();
			if (string.IsNullOrWhiteSpace(address))
				return false;

			string value = address.ToUpper();
			if (!IsHex(value))
				return false;

			addressComponents = address.Split(':');
			if (addressComponents.Length != HEXTET_PER_ADDRESS)
				return false;

			return true;
		}

		public static bool IsHex(string address)
		{
			foreach (var c in address)
			{
				if ((c < ZERO_ASCII || c > NINE_ACSII) && (c < UPPERCASE_A_ASCII || c > UPPERCASE_F_ASCII) && c != COLON_ASCII)
					return false;
			}
			return true;
		}

		public static string[] Expand(string[] compressedAddress)
		{
			var result = new string[HEXTET_PER_ADDRESS];
			var removedComponents = HEXTET_PER_ADDRESS - compressedAddress.Length;

			for (int i = 0, j = 0; i < compressedAddress.Length; i++, j++)
			{
				if (compressedAddress[i] == "")
					for (int k = 0; k < removedComponents; k++, j++)
						result[j] = "0";
				else
					result[j] = compressedAddress[i];
			}

			return result;
		}

		private static string OmitLeadingOs(string addressSegment, byte index, bool[] isOSegment)
		{
			if (!addressSegment[0].Equals('0'))
				return addressSegment;

			if (addressSegment.Length == 1)
			{
				if (addressSegment[0].Equals('0'))
					isOSegment[index] = true;

				return addressSegment;
			}
			return OmitLeadingOs(addressSegment.Substring(1), index, isOSegment);
		}
	}
}
