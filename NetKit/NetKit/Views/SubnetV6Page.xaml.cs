using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SubnetV6Page : ContentPage
	{
		readonly string[] baseAddress = new string[8];
		byte globalRoutingPrefix = 0;
		byte subnetId = 0;
		ushort howMany = 0;

		public readonly ObservableCollection<string> Addresses = new ObservableCollection<string>();

		readonly SemaphoreSlim semaphore = new SemaphoreSlim(0);
		Task processData = new Task(() => { });
		CancellationTokenSource tokenSource = new CancellationTokenSource();
		CancellationToken token;


		public SubnetV6Page()
		{
			InitializeComponent();
			token = tokenSource.Token;
			subnetListView.ItemsSource = Addresses;
		}

		void SubmitClicked(object sender, EventArgs e)
		{
			try
			{
				// Read data from User Input
				if (!GetBaseAddress())
					return;
				if (string.IsNullOrWhiteSpace(globalPrefixEntry.Text))
				{
					DisplayError("Global Routing Prefix must not be empty!");
					return;
				}

				globalRoutingPrefix = byte.Parse(globalPrefixEntry.Text);
				subnetId = (byte)(16 - (globalRoutingPrefix % 16));

				if (string.IsNullOrWhiteSpace(howManyEntry.Text))
				{
					DisplayError("Subnet number must not be empty!");
					return;
				}

				howMany = ushort.Parse(howManyEntry.Text);
				byte index = (byte)((globalRoutingPrefix + subnetId) / 16 - 1);
				string lastDigit = $"{baseAddress[index].Last()}";
				byte baseValue = byte.Parse(lastDigit,
					System.Globalization.NumberStyles.HexNumber);

				if (howMany > (1 << subnetId - baseValue))
				{
					DisplayError("Subnet Id is too small for this many subnets");
					return;
				}
				else if (howMany > short.MaxValue)
				{
					DisplayAlert("Alert", $"You are trying to get {howMany} " +
						$"subnets, it will take a while", "I'll wait");
				}

				_ = Submit(howMany);
			}
			catch (Exception)
			{
				DisplayAlert("Error", "Entered data is not valid", "OK");
			}
		}

		bool GetBaseAddress()
		{
			if (string.IsNullOrWhiteSpace(baseAddressEntry.Text))
			{
				DisplayError("Base Address must not be empty!");
				return false;
			}

			foreach (var c in baseAddressEntry.Text.ToUpper())
			{
				if ((c < 48 || c > 57) && (c < 65 || c > 90) && c != ':')
				{
					DisplayError("Base Address must be Hex");
					return false;
				}
			}

			string[] address;
			baseAddressEntry.Text = baseAddressEntry.Text.Trim();
			if (baseAddressEntry.Text.EndsWith("::"))
			{
				address = baseAddressEntry.Text.Remove(baseAddressEntry.Text.LastIndexOf(":")).Split(':');
			}
			else
			{
				address = baseAddressEntry.Text.Split(':');
				if (address.Length != 8)
				{
					DisplayError("Base address in not in the correct format!");
					return false;
				}
			}

			byte len = (byte)address.Length;
			for (byte i = 0, j = 0; i < len; i++)
			{
				if (address[i] != "")
				{
					baseAddress[j] = address[i];
					j++;
				}
				else
				{
					byte n = (byte)(9 - len);
					for (byte k = 0; k < n; k++)
						baseAddress[j + k] = "0";
					j += n;
				}
			}
			return true;
		}

		string GetFormattedAddress(uint value, byte index)
		{
			string[] temp = new string[8];
			for (byte i = 0; i < 8; i++)
			{
				if (i != index)
					temp[i] = baseAddress[i];
				else
					temp[i] = Convert.ToString(value, 16);
			}

			return CompressPage.CompressIP(temp, 8, new bool[8]);
		}

		async Task Submit(uint howMany)
		{
			try
			{
				if (processData.Status == TaskStatus.Running)
				{
					tokenSource.Cancel();
					await Task.Run(() => semaphore.Wait());
				}

				processData = Task.Run(() =>
				{
					byte index = (byte)((globalRoutingPrefix + subnetId) / 16 - 1);

					ushort address = ushort.Parse(baseAddress[index],
						System.Globalization.NumberStyles.HexNumber);
					Addresses.Clear();
					for (ushort i = 0; i < howMany; i++)
					{
						token.ThrowIfCancellationRequested();
						Addresses.Add(GetFormattedAddress(address, index));
						address++;
					}
					token.ThrowIfCancellationRequested();
				}, token);
				await processData;
			}
			catch (Exception)
			{
				tokenSource = new CancellationTokenSource();
				token = tokenSource.Token;
			}
			finally
			{
				semaphore.Release();
			}
		}

		void DisplayError(string error)
		{
			DisplayAlert("Error", error, "OK");
		}
	}
}