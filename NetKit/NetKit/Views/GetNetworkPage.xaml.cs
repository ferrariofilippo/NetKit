using NetKit.Services;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class GetNetworkPage : ContentPage
	{
		private readonly byte[] network = new byte[4];
		private readonly byte[] host = new byte[4];
		private readonly byte[] mask = new byte[4];
		private byte prefixLength;

		public GetNetworkPage()
		{
			InitializeComponent();
		}

		private async void Submit(object sender, EventArgs e)
		{
			if (!await Task.Run(() => IPv4Helpers.TryParseAddress(hostEntry.Text, host)))
			{
				await DisplayAlert("Error", "Host Address is not valid!", "OK");
				return;
			}
			if (!await Task.Run(() => GetNetworkAddress()))
			{
				await DisplayAlert("Error", "Subnet Mask is not valid!", "OK");
				return;
			}
			outputLabel.Text = $"Network Address: {network[0]}.{network[1]}" +
				$".{network[2]}.{network[3]}";
		}

		private bool GetNetworkAddress()
		{
			string value = maskEntry.Text;
			if (string.IsNullOrWhiteSpace(value))
				return false;

			if (value.StartsWith("\\") || value.StartsWith("/"))
			{
				if (!byte.TryParse(value.Substring(1), out prefixLength) || 
					!IPv4Helpers.TryGetSubnetMask(prefixLength, mask))
				{
					return false;
				}
			}
			else if (!IPv4Helpers.TryParseAddress(value, mask))
			{
				return false;
			}

			for (int i = 0; i < 4; i++)
				network[i] = (byte)(host[i] & mask[i]);
			return true;
		}
	}
}
