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
		private readonly byte[] lastUsed = { 10, 0, 0, 0 };
		private readonly List<Network> calculatedNetworks = new List<Network>();
		private readonly List<uint> maxValues = new List<uint>();
		private List<uint> values;
		private int height = 30;
		private int howManySubnets = 0;

		public VLSMPage()
		{
			InitializeComponent();

			Task t = Task.Run(() => IPv4Helpers.GetSubnetMaxHosts(maxValues));
			hostsListView.ItemsSource = addedItems;
			outputListView.ItemsSource = networks;
			outputListView.HeightRequest = 10;
			Add_Clicked(null, EventArgs.Empty);

			t.Wait();
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
					GetPrefixes();
					GetSubnets();
					GetNetworks();
					GetBroadcast();
				});
				outputListView.HeightRequest = 30 + outputListView.RowHeight * calculatedNetworks.Count;
				networks.Clear();
				foreach (var item in calculatedNetworks)
					networks.Add(item);

				calculatedNetworks.Clear();
				for (int i = 0; i < 4; i++)
					lastUsed[i] = 0;
			}
			catch (Exception)
			{
				await DisplayAlert("Error", "Entered data is not in the right format!", "OK");
			}
		}

		private List<uint> GetValues()
		{
			List<uint> data = new List<uint>();
			foreach (var item in addedItems)
			{
				if (item.Data.StartsWith("/") || item.Data.StartsWith("\\"))
				{
					byte prefixLength = byte.Parse(item.Data.Substring(1));  // Get Prefix Length after removing '/' or '\\'
					if (TryGetMinimumWasteHostSize(prefixLength, out uint size))
					{
						data.Add(size);
					}
					else
					{
						DisplayAlert("Error", "Invalid Prefix Length!", "OK");
						return null;
					}
				}
				else
				{
					if (TryGetMinimumWasteHostSize(uint.Parse(item.Data), out uint size))
					{
						data.Add(size);
					}
					else
					{
						DisplayAlert("Error", "Too many hosts!", "OK");
						return null;
					}
				}
			}
			data.OrderByDescending(x=> x);
			return data;
		}

		private bool TryGetMinimumWasteHostSize(byte prefixLength, out uint size)
		{
			size = 0;
			sbyte howManyCycles = (sbyte)(30 - prefixLength);
			if (howManyCycles >= 0)
			{
				size = maxValues[howManyCycles];
				return true;
			}
			return false;
		}

		private bool TryGetMinimumWasteHostSize(uint hosts, out uint size)
		{
			size = 0;
			byte index = 0;
			while (index < maxValues.Count && hosts > maxValues[index])
				index++;
			if (index >= maxValues.Count)
				return false;

			size = maxValues[index];
			return true;
		}

		private void GetPrefixes()
		{
			for (int i = 0; i < howManySubnets; i++)
			{
				byte prefix = 0;
				while (maxValues[prefix] < values[i])
					prefix++;

				prefix = (byte)(30 - prefix);
				calculatedNetworks.Add(new Network()
				{
					PrefixLength = prefix,
					HostNumber = values[i]
				});
			}
		}

		private void GetSubnets()
		{
			byte[] mask;
			for (int i = 0; i < howManySubnets; i++)
			{
				mask = IPv4Helpers.GetSubnetMask(calculatedNetworks[i].PrefixLength);
				calculatedNetworks[i].SubnetMask = $"{mask[0]}.{mask[1]}.{mask[2]}.{mask[3]}";
			}
		}

		private void GetNetworks()
		{
			if (calculatedNetworks[0].PrefixLength > 24)
			{
				lastUsed[0] = 192;
				lastUsed[1] = 168;
			}
			else if (calculatedNetworks[0].PrefixLength > 16)
			{
				lastUsed[0] = 172;
				lastUsed[1] = 16;
			}
			else if (calculatedNetworks[0].PrefixLength <= 8)
			{
				lastUsed[0] = 0;
			}

			for (int i = 0; i < howManySubnets; i++)
			{
				calculatedNetworks[i].NetworkAddress = $"{lastUsed[0]}.{lastUsed[1]}.{lastUsed[2]}.{lastUsed[3]}";
				if (calculatedNetworks[i].PrefixLength > 24)
					lastUsed[3] += (byte)MathHelpers.PowerOfTwo(32 - calculatedNetworks[i].PrefixLength);
				else if (calculatedNetworks[i].PrefixLength > 16)
					lastUsed[2] += (byte)MathHelpers.PowerOfTwo(24 - calculatedNetworks[i].PrefixLength);
				else if (calculatedNetworks[i].PrefixLength > 8)
					lastUsed[1] += (byte)MathHelpers.PowerOfTwo(16 - calculatedNetworks[i].PrefixLength);
				else
					lastUsed[0] += (byte)MathHelpers.PowerOfTwo(8 - calculatedNetworks[i].PrefixLength);
			}
		}

		private void GetBroadcast()
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
