using System;
using System.Text;

namespace NetKit.Services
{
	public static class IPv6Helpers
	{
		public static string Compress(ref string[] addressComponents, byte length)
		{
			bool[] addressIsO = new bool[8];
			byte[] parameters = new byte[2];
			StringBuilder output = new StringBuilder();

			for (byte i = 0; i < length; i++)
			{
				if (!addressComponents[i].Equals(""))
					addressComponents[i] = OmitLeadingOs(addressComponents[i], i, addressIsO);
				else
					addressComponents[i] = "0";
			}
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
					if (i != 7)
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
			if (addressComponents.Length != 8)
				return false;

			return true;
		}

		public static bool IsHex(string address)
		{
			foreach (var c in address)
			{
				if ((c < 48 || c > 57) && (c < 65 || c > 70) && c != 58)
					return false;
			}
			return true;
		}

		public static string[] Expand(string[] compressedAddress)
		{
			string[] result = new string[8];
			int removedComponents = 8 - compressedAddress.Length;

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
