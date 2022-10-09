using CommunityToolkit.Mvvm.ComponentModel;

namespace NetKit.ViewModels
{
	public class GetNetworkViewModel : ObservableObject
	{
		private string hostNumber;
		public string HostNumber
		{
			get => hostNumber;
			set => SetProperty(ref hostNumber, value);
		}

		private string subnetMask;
		public string SubnetMask
		{
			get => subnetMask;
			set => SetProperty(ref subnetMask, value);
		}

		private string networkAddress;
		public string NetworkAddress
		{
			get => networkAddress;
			set => SetProperty(ref networkAddress, value);
		}
	}
}
