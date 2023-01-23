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
		private const int HEXTET_SIZE = 16;
		private const int HEXTET_PER_ADDRESS = 8;
		private const int BYTE_SIZE = 8;

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
				subnetId = (byte)(HEXTET_SIZE - (globalRoutingPrefix % HEXTET_SIZE));

				if (string.IsNullOrWhiteSpace(viewModel.SubnetNumber))
				{
					DisplayError("Subnet number must not be empty!");
					return;
				}

				var howMany = ushort.Parse(viewModel.SubnetNumber);
				var index = (byte)((globalRoutingPrefix + subnetId) / HEXTET_SIZE - 1);
				var lastDigit = $"{baseAddress[index].Last()}";
				var baseValue = byte.Parse(lastDigit,
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

				if (baseAddress.Length != HEXTET_PER_ADDRESS)
				{
					DisplayError("Base address in not in the correct format!");
					return false;
				}
			}

			return true;
		}

		private string GetFormattedAddress(uint value, byte index)
		{
			var temp = new string[HEXTET_PER_ADDRESS];
			for (byte i = 0; i < HEXTET_PER_ADDRESS; i++)
				temp[i] = i != index ? baseAddress[i] : Convert.ToString(value, HEXTET_SIZE);

			return IPv6Helpers.Compress(ref temp, HEXTET_PER_ADDRESS);
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
					var index = (byte)((globalRoutingPrefix + subnetId) / HEXTET_SIZE - 1);
					var address = ushort.Parse(baseAddress[index],
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
