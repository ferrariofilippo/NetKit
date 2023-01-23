namespace NetKit.Services
{
	public static class MathHelpers
	{
		private const int BITS_PER_ADDRESS = 32;

		public static readonly uint[] PowersOfTwo = new uint[BITS_PER_ADDRESS];

		public static void Init()
		{
			for (int i = 0; i < PowersOfTwo.Length; i++)
				PowersOfTwo[i] = 1u << i;
		}

		public static int GetExcessBase2Log(uint n)
		{
			var index = 0;
			while (n > PowersOfTwo[index++]) ;
			return --index;
		}

		public static int GetDefectBase2Log(uint n)
		{
			var index = BITS_PER_ADDRESS;
			while (n < PowersOfTwo[--index]) ;
			return index;
		}
	}
}
