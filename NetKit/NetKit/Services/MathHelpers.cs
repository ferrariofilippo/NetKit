namespace NetKit.Services
{
	public static class MathHelpers
	{
		public static readonly uint[] PowersOfTwo = new uint[32];

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
			var index = 32;
			while (n < PowersOfTwo[--index]) ;
			return index;
		}
	}
}
