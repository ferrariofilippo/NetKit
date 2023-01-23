using NetKit.Model;
using NetKit.Services;
using NetKit.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class VLSMPage : ContentPage
	{
		private const int BITS_PER_ADDRESS = 32;
		private const int BITS_PER_BYTE = 8;
		private const int BYTES_PER_ADDRESS = 4;
		private const int INPUT_MINIMUM_HEIGHT = 30;

		private readonly VLSMViewModel viewModel;
		private readonly byte[] lastSubnet = { 10, 0, 0, 0 };
		private readonly List<Network> calculatedNetworks = new List<Network>();
		private List<uint> values;
		private int howManySubnets = 0;

		public VLSMPage()
		{
			InitializeComponent();

			viewModel = new VLSMViewModel();
			BindingContext = viewModel;

			networkListView.HeightRequest = 10;

			Add_Clicked(null, null);
		}

		private void Add_Clicked(object sender, EventArgs e)
		{
			viewModel.ExpandInputListView();
		}

		private void Delete_Clicked(object sender, EventArgs e)
		{
			viewModel.ContractInputListView();
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
				networkListView.HeightRequest = INPUT_MINIMUM_HEIGHT + networkListView.RowHeight * calculatedNetworks.Count;
				viewModel.Networks.Clear();
				foreach (var item in calculatedNetworks)
					viewModel.Networks.Add(item);

				calculatedNetworks.Clear();
				lastSubnet[0] = 10;
				lastSubnet[1] = 0;
				lastSubnet[2] = 0;
				lastSubnet[3] = 0;
			}
			catch (Exception)
			{
				await DisplayAlert("Error", "Entered data is not in the right format!", "OK");
			}
		}

		private List<uint> GetValues()
		{
			var data = new List<uint>();
			uint size;
			foreach (var item in viewModel.AddedItems)
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
				mask = new byte[BYTES_PER_ADDRESS];
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
				{
					lastSubnet[3] += (byte)MathHelpers.PowersOfTwo[32 - calculatedNetworks[i].PrefixLength];
					if (lastSubnet[3] == 0)
						lastSubnet[2]++;
				}
				else if (calculatedNetworks[i].PrefixLength > 16)
				{
					lastSubnet[2] += (byte)MathHelpers.PowersOfTwo[24 - calculatedNetworks[i].PrefixLength];
					if (lastSubnet[2] == 0)
						lastSubnet[1]++;
				}
				else if (calculatedNetworks[i].PrefixLength > 8)
				{
					lastSubnet[1] += (byte)MathHelpers.PowersOfTwo[16 - calculatedNetworks[i].PrefixLength];
					if (lastSubnet[1] == 0)
						lastSubnet[0]++;
				}
				else
					lastSubnet[0] += (byte)MathHelpers.PowersOfTwo[8 - calculatedNetworks[i].PrefixLength];
			}
		}

		private void SetBroadcast()
		{
			var ip = new byte[BYTES_PER_ADDRESS];
			for (int i = 0; i < howManySubnets; i++)
			{
				byte hostBits = (byte)(BITS_PER_ADDRESS - calculatedNetworks[i].PrefixLength);
				byte cont = 0;
				foreach (var item in calculatedNetworks[i].NetworkAddress.Split('.'))
				{
					ip[cont++] = byte.Parse(item);
				}
				for (int j = 0; j < BYTES_PER_ADDRESS; j++)
				{
					for (int k = 0; k < BITS_PER_BYTE; k++)
					{
						if (j * BITS_PER_BYTE + k < hostBits)
							ip[3 - j] |= (byte)MathHelpers.PowersOfTwo[k];
						else
							break;
					}
				}
				calculatedNetworks[i].BroadcastAddress = $"{ip[0]}.{ip[1]}.{ip[2]}.{ip[3]}";
			}
		}
	}
}
