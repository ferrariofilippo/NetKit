using CommunityToolkit.Mvvm.ComponentModel;

namespace NetKit.ViewModels
{
	public class NthSubnetViewModel : ObservableObject
	{
		private string subnetMask;
		public string SubnetMask
		{
			get => subnetMask;
			set => SetProperty(ref subnetMask, value);
		}

		private string subnetNumber;
		public string SubnetNumber
		{ 
			get => subnetNumber;
			set => SetProperty(ref subnetNumber, value);
		}

		private string networkAddress;
		public string NetworkAddress
		{
			get => networkAddress;
			set => SetProperty(ref networkAddress, value);
		}
	}
}
