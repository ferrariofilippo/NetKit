namespace NetKit.Services
{
	public static class MathHelpers
	{
		public static uint PowerOfTwo(int exponent)
		{
			uint temp = 1;
			return temp << exponent;
		}
	}
}
