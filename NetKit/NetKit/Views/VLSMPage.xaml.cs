using NetKit.Model;
using NetKit.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class VLSMPage : ContentPage
	{
		private readonly ObservableCollection<HostLine> addedItems = new ObservableCollection<HostLine>();
		private readonly ObservableCollection<Network> networks = new ObservableCollection<Network>();
		private readonly byte[] lastSubnet = { 10, 0, 0, 0 };
		private readonly List<Network> calculatedNetworks = new List<Network>();
		private List<uint> values;
		private int height = 30;
		private int howManySubnets = 0;

		public VLSMPage()
		{
			InitializeComponent();

			hostsListView.ItemsSource = addedItems;
			outputListView.ItemsSource = networks;
			outputListView.HeightRequest = 10;

			Add_Clicked(null, null);
		}

		private void Add_Clicked(object sender, EventArgs e)
		{
			addedItems.Add(new HostLine());
			height += hostsListView.RowHeight;
			hostsListView.HeightRequest = height;
		}

		private void Delete_Clicked(object sender, EventArgs e)
		{
			int lastIndex = addedItems.Count - 1;
			if (lastIndex <= 0)
				return;
			addedItems.RemoveAt(lastIndex);
			height -= hostsListView.RowHeight;
			hostsListView.HeightRequest = height;
		}

		private async void GetResults_Clicked(object sender, EventArgs e)
		{
			try
			{
				await Task.Run(() =>
				{
					values = GetValues();
					if (values is null)
						return;

					howManySubnets = values.Count;
					SetPrefixes();
					SetSubnets();
					SetNetworks();
					SetBroadcast();
				});
				outputListView.HeightRequest = 30 + outputListView.RowHeight * calculatedNetworks.Count;
				networks.Clear();
				foreach (var item in calculatedNetworks)
					networks.Add(item);

				calculatedNetworks.Clear();
				lastSubnet[0] = 10;
				for (int i = 1; i < 4; i++)
					lastSubnet[i] = 0;
			}
			catch (Exception)
			{
				await DisplayAlert("Error", "Entered data is not in the right format!", "OK");
			}
		}

		private List<uint> GetValues()
		{
			List<uint> data = new List<uint>();
			uint size;
			foreach (var item in addedItems)
			{
				if (item.Data.StartsWith("/") || item.Data.StartsWith("\\"))
					size = IPv4Helpers.GetMinimumWasteHostSize(byte.Parse(item.Data.Substring(1)));
				else
					size = IPv4Helpers.GetMinimumWasteHostSize(uint.Parse(item.Data));
				
				if (size != 0)
				{
					data.Add(size);
				}
				else
				{
					DisplayAlert("Error", "Hosts or Prefix Length is not valid", "OK");
					return null;
				}
			}
			return data.OrderByDescending(x => x).ToList();
		}	

		private void SetPrefixes()
		{
			for (int i = 0; i < howManySubnets; i++)
			{
				calculatedNetworks.Add(new Network()
				{
					PrefixLength = IPv4Helpers.GetPrefixLength(values[i]),
					HostNumber = values[i]
				});
			}
		}

		private void SetSubnets()
		{
			byte[] mask;
			for (int i = 0; i < howManySubnets; i++)
			{
				mask = new byte[4];
				IPv4Helpers.TryGetSubnetMask(calculatedNetworks[i].PrefixLength, mask);
				calculatedNetworks[i].SubnetMask = $"{mask[0]}.{mask[1]}.{mask[2]}.{mask[3]}";
			}
		}

		private void SetNetworks()
		{
			if (calculatedNetworks[0].PrefixLength > 24)
			{
				lastSubnet[0] = 192;
				lastSubnet[1] = 168;
			}
			else if (calculatedNetworks[0].PrefixLength > 16)
			{
				lastSubnet[0] = 172;
				lastSubnet[1] = 16;
			}
			else if (calculatedNetworks[0].PrefixLength <= 8)
			{
				lastSubnet[0] = 0;
			}

			for (int i = 0; i < howManySubnets; i++)
			{
				calculatedNetworks[i].NetworkAddress = $"{lastSubnet[0]}.{lastSubnet[1]}.{lastSubnet[2]}.{lastSubnet[3]}";
				if (calculatedNetworks[i].PrefixLength > 24)
					lastSubnet[3] += (byte)MathHelpers.PowerOfTwo(32 - calculatedNetworks[i].PrefixLength);
				else if (calculatedNetworks[i].PrefixLength > 16)
					lastSubnet[2] += (byte)MathHelpers.PowerOfTwo(24 - calculatedNetworks[i].PrefixLength);
				else if (calculatedNetworks[i].PrefixLength > 8)
					lastSubnet[1] += (byte)MathHelpers.PowerOfTwo(16 - calculatedNetworks[i].PrefixLength);
				else
					lastSubnet[0] += (byte)MathHelpers.PowerOfTwo(8 - calculatedNetworks[i].PrefixLength);
			}
		}

		private void SetBroadcast()
		{
			byte[] ip = new byte[4];
			for (int i = 0; i < howManySubnets; i++)
			{
				byte hostBits = (byte)(32 - calculatedNetworks[i].PrefixLength);
				byte cont = 0;
				foreach (var item in calculatedNetworks[i].NetworkAddress.Split('.'))
				{
					ip[cont] = byte.Parse(item);
					cont++;
				}
				for (int j = 0; j < 4; j++)
				{
					for (int k = 0; k < 8; k++)
					{
						if (j * 8 + k < hostBits)
							ip[3 - j] += (byte)MathHelpers.PowerOfTwo(k);
						else
							break;
					}
				}
				calculatedNetworks[i].BroadcastAddress = $"{ip[0]}.{ip[1]}.{ip[2]}.{ip[3]}";
			}
		}
	}
}
