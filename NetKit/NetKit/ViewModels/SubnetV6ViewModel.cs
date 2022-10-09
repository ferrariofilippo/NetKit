using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace NetKit.ViewModels
{
	public class SubnetV6ViewModel : ObservableObject
	{
		private string baseAddress;
		public string BaseAddress
		{
			get => baseAddress;
			set => SetProperty(ref baseAddress, value);
		}

		private string globalRoutingPrefix;
		public string GlobalRoutingPrefix
		{
			get => globalRoutingPrefix;
			set => SetProperty(ref globalRoutingPrefix, value);
		}

		private string subnetNumber;
		public string SubnetNumber
		{
			get => subnetNumber;
			set => SetProperty(ref subnetNumber, value);
		}

		public ObservableCollection<string> Addresses { get; private set; } = new ObservableCollection<string>();
	}
}
