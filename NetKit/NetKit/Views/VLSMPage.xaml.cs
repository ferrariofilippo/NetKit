using NetKit.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class VLSMPage : ContentPage
	{
		readonly ObservableCollection<HostLine> addedItems = new ObservableCollection<HostLine>();
		readonly ObservableCollection<Network> networks = new ObservableCollection<Network>();
		readonly List<Network> calculatedNetworks = new List<Network>();
		int height = 30;

		readonly byte[] lastUsed = { 10, 0, 0, 0 };
		readonly List<uint> maxValues = new List<uint>();
		List<uint> values;
		int len = 0;

		public VLSMPage()
		{
			InitializeComponent();

			Task t = Task.Run(() => Get_MaxValues());
			hostsListView.ItemsSource = addedItems;
			outputListView.ItemsSource = networks;
			outputListView.HeightRequest = 10;
			Add_Clicked(null, EventArgs.Empty);

			t.Wait();
		}

		void Add_Clicked(object sender, EventArgs e)
		{
			addedItems.Add(new HostLine());
			height += hostsListView.RowHeight;
			hostsListView.HeightRequest = height;
		}

		void Delete_Clicked(object sender, EventArgs e)
		{
			int lastIndex = addedItems.Count - 1;
			if (lastIndex <= 0)
				return;
			addedItems.RemoveAt(lastIndex);
			height -= hostsListView.RowHeight;
			hostsListView.HeightRequest = height;
		}

		async void GetResults_Clicked(object sender, EventArgs e)
		{
			try
			{
				await Task.Run(() =>
				{
					values = Get_Values();
					if (values is null)
						return;

					len = values.Count;
					Get_Prefixes();
					Get_Subnets();
					Get_Networks();
					Get_Broadcast();
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

		List<uint> Get_Values()
		{
			List<uint> data = new List<uint>();
			foreach (var item in addedItems)
			{
				if (item.Data.StartsWith("/") || item.Data.StartsWith("\\"))
				{
					if (GetSize(item.Data, out uint size))
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
					data.Add(uint.Parse(item.Data));
				}
			}
			return data;
		}

		bool GetSize(string data, out uint s)
		{
			s = 0;
			data = data.Substring(1);
			byte size = byte.Parse(data);
			sbyte cycles = (sbyte)(30 - size);
			if (cycles >= 0)
			{
				s = maxValues[cycles];
				return true;
			}

			return false;
		}

		public static uint PowerOfTwo(int y)
		{
			uint temp = 1;
			return temp << y;
		}

		void Get_Prefixes()
		{
			for (int i = 0; i < len; i++)
			{
				byte j = 0;
				while (values[i] > maxValues[j])
					j++;

				values[i] = maxValues[j];
			}
			values.Sort();
			values.Reverse();

			for (int i = 0; i < len; i++)
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

		void Get_Subnets()
		{
			byte[] mask;
			for (int i = 0; i < len; i++)
			{
				mask = Subnet(calculatedNetworks[i].PrefixLength);
				calculatedNetworks[i].SubnetMask = $"{mask[0]}.{mask[1]}.{mask[2]}.{mask[3]}";
			}
		}

		public static byte[] Subnet(byte prefix)
		{
			byte[] mask = new byte[4];
			for (int j = 0; j < 4; j++)
			{
				for (int k = 0; k < 8; k++)
				{
					if (j * 8 + k < prefix)
					{
						mask[j] += (byte)PowerOfTwo(7 - k);
					}
					else
					{
						k = 9;
						j = 5;
					}
				}
			}
			return mask;
		}

		void Get_Networks()
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

			for (int i = 0; i < len; i++)
			{
				calculatedNetworks[i].NetworkAddress = $"{lastUsed[0]}.{lastUsed[1]}.{lastUsed[2]}.{lastUsed[3]}";
				if (calculatedNetworks[i].PrefixLength > 24)
					lastUsed[3] += (byte)PowerOfTwo(32 - calculatedNetworks[i].PrefixLength);
				else if (calculatedNetworks[i].PrefixLength > 16)
					lastUsed[2] += (byte)PowerOfTwo(24 - calculatedNetworks[i].PrefixLength);
				else if (calculatedNetworks[i].PrefixLength > 8)
					lastUsed[1] += (byte)PowerOfTwo(16 - calculatedNetworks[i].PrefixLength);
				else
					lastUsed[0] += (byte)PowerOfTwo(8 - calculatedNetworks[i].PrefixLength);
			}
		}

		void Get_Broadcast()
		{
			byte[] ip = new byte[4];
			for (int i = 0; i < len; i++)
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
						{
							ip[3 - j] += (byte)PowerOfTwo(k);
						}
						else
						{
							k = 9;
							j = 5;
						}
					}
				}
				calculatedNetworks[i].BroadcastAddress = $"{ip[0]}.{ip[1]}.{ip[2]}.{ip[3]}";
			}
		}

		void Get_MaxValues()
		{
			maxValues.Add(4);
			for (int i = 0; i < 30; i++)
				maxValues.Add(maxValues[i] << 1);
			for (int i = 0; i < maxValues.Count; i++)
				maxValues[i] -= 2;
		}
	}
}
