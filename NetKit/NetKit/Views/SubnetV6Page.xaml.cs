using NetKit.Services;
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
		private readonly SemaphoreSlim semaphore = new SemaphoreSlim(0);
		public readonly ObservableCollection<string> Addresses = new ObservableCollection<string>();
		
		private string[] baseAddress;
		private byte globalRoutingPrefix = 0;
		private byte subnetId = 0;
		private Task processData = Task.CompletedTask;
		private CancellationTokenSource tokenSource = new CancellationTokenSource();
		private CancellationToken token;

		public SubnetV6Page()
		{
			InitializeComponent();
			token = tokenSource.Token;
			subnetListView.ItemsSource = Addresses;
		}

		private void SubmitClicked(object sender, EventArgs e)
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

				ushort howMany = ushort.Parse(howManyEntry.Text);
				byte index = (byte)((globalRoutingPrefix + subnetId) / 16 - 1);
				string lastDigit = $"{baseAddress[index].Last()}";
				byte baseValue = byte.Parse(lastDigit,
					System.Globalization.NumberStyles.HexNumber);

				if (howMany > (1 << subnetId - baseValue))
				{
					DisplayError("Subnet Id is too small for these many subnets");
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

		private bool GetBaseAddress()
		{
			if (string.IsNullOrWhiteSpace(baseAddressEntry.Text))
			{
				DisplayError("Base Address must not be empty!");
				return false;
			}

			baseAddressEntry.Text = baseAddressEntry.Text.Trim();

			if (!IPv6Helpers.IsHex(baseAddressEntry.Text.ToUpper()))
			{
				DisplayError("Base Address must be Hex");
				return false;
			}

			if (baseAddressEntry.Text.EndsWith("::"))
			{
				baseAddress = IPv6Helpers.Expand(baseAddressEntry.Text.Remove(baseAddressEntry.Text.Length - 1).Split(':'));
			}
			else
			{
				if (baseAddressEntry.Text.Contains("::"))
					baseAddress = IPv6Helpers.Expand(baseAddressEntry.Text.Split(':'));
				else
					baseAddress = baseAddressEntry.Text.Split(':');

				if (baseAddress.Length != 8)
				{
					DisplayError("Base address in not in the correct format!");
					return false;
				}
			}

			return true;
		}

		private string GetFormattedAddress(uint value, byte index)
		{
			string[] temp = new string[8];
			for (byte i = 0; i < 8; i++)
			{
				if (i != index)
					temp[i] = baseAddress[i];
				else
					temp[i] = Convert.ToString(value, 16);
			}

			return IPv6Helpers.Compress(ref temp, 8);
		}

		private async Task Submit(ushort howMany)
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
					for (ushort i = 0; i < howMany; i++, address++)
					{
						token.ThrowIfCancellationRequested();
						Addresses.Add(GetFormattedAddress(address, index));
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

		private void DisplayError(string error)
		{
			DisplayAlert("Error", error, "OK");
		}
	}
}
