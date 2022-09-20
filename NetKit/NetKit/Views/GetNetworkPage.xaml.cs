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
        private byte[] mask = new byte[4];
        private byte prefixLength;

        public GetNetworkPage()
        {
            InitializeComponent();
        }

        private async void Submit(object sender, EventArgs e)
        {
            if (!await Task.Run(() => GetHost()))
            {
                await DisplayAlert("Error", "Host Address is not valid!", "OK");
                return;
            }
            if (!await Task.Run(() => Get_Network()))
            {
                await DisplayAlert("Error", "Subnet Mask is not valid!", "OK");
                return;
            }
            outputLabel.Text = $"Network Address: {network[0]}.{network[1]}" +
                $".{network[2]}.{network[3]}";
        }

        private bool GetHost()
        {
            string value = hostEntry.Text;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string[] fields = value.Split('.');
            if (fields.Length != 4)
                return false;

            for (int i = 0; i < 4; i++)
            {
                if (!byte.TryParse(fields[i], out host[i]))
                    return false;
            }
            return true;
        }

        private bool Get_Network()
        {
            string value = maskEntry.Text;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (value.StartsWith("\\") || value.StartsWith("/"))
            {
                if (!byte.TryParse(value.Substring(1), out prefixLength))
                    return false;

                mask = IPv4Helpers.GetSubnetMask(prefixLength);
            }
            else
            {
                string[] fields = value.Split('.');
                if (fields.Length != 4)
                    return false;

                for (int i = 0; i < 4; i++)
                {
                    if (!byte.TryParse(fields[i], out mask[i]))
                        return false;
                }
            }

            for (int i = 0; i < 4; i++)
                network[i] = (byte)(host[i] & mask[i]);
            return true;
        }
    }
}
