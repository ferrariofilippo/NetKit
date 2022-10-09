using CommunityToolkit.Mvvm.ComponentModel;

namespace NetKit.ViewModels
{
	public class CompressViewModel : ObservableObject
	{
		private string ipAddress;
		public string IpAddress
		{
			get => ipAddress;
			set => SetProperty(ref ipAddress, value);
		}

		private string outputAddress;
		public string OutputAddress
		{
			get => outputAddress;
			set => SetProperty(ref outputAddress, value);
		}
	}
}
