using NetKit.Services;
using NetKit.ViewModels;
using System;
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
		private readonly SubnetV6ViewModel viewModel;
		
		private string[] baseAddress;
		private byte globalRoutingPrefix = 0;
		private byte subnetId = 0;
		private Task processData = Task.CompletedTask;
		private CancellationTokenSource tokenSource = new CancellationTokenSource();
		private CancellationToken token;

		public SubnetV6Page()
		{
			InitializeComponent();
			viewModel = new SubnetV6ViewModel();
			BindingContext = viewModel;
			token = tokenSource.Token;
		}

		private void SubmitClicked(object sender, EventArgs e)
		{
			try
			{
				// Read data from User Input
				if (!GetBaseAddress())
					return;
				if (string.IsNullOrWhiteSpace(viewModel.GlobalRoutingPrefix))
				{
					DisplayError("Global Routing Prefix must not be empty!");
					return;
				}

				globalRoutingPrefix = byte.Parse(viewModel.GlobalRoutingPrefix);
				subnetId = (byte)(16 - (globalRoutingPrefix % 16));

				if (string.IsNullOrWhiteSpace(viewModel.SubnetNumber))
				{
					DisplayError("Subnet number must not be empty!");
					return;
				}

				ushort howMany = ushort.Parse(viewModel.SubnetNumber);
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
			if (string.IsNullOrWhiteSpace(viewModel.BaseAddress))
			{
				DisplayError("Base Address must not be empty!");
				return false;
			}

			viewModel.BaseAddress = viewModel.BaseAddress.Trim();

			if (!IPv6Helpers.IsHex(viewModel.BaseAddress.ToUpper()))
			{
				DisplayError("Base Address must be Hex");
				return false;
			}

			if (viewModel.BaseAddress.EndsWith("::"))
			{
				baseAddress = IPv6Helpers.Expand(viewModel.BaseAddress.Remove(viewModel.BaseAddress.Length - 1).Split(':'));
			}
			else
			{
				if (viewModel.BaseAddress.Contains("::"))
					baseAddress = IPv6Helpers.Expand(viewModel.BaseAddress.Split(':'));
				else
					baseAddress = viewModel.BaseAddress.Split(':');

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
					viewModel.Addresses.Clear();
					for (ushort i = 0; i < howMany; i++, address++)
					{
						token.ThrowIfCancellationRequested();
						viewModel.Addresses.Add(GetFormattedAddress(address, index));
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
