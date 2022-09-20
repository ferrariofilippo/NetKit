using NetKit.Services;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CompressPage : ContentPage
    {
        private string[] address;

        public CompressPage()
        {
            InitializeComponent();
        }

        private async void CompressClicked(object sender, EventArgs e)
        {
            if (!IPv6Helpers.ValidateAddress(ipEntry.Text, out address))
            {
                await DisplayAlert("Error", "IP address entered is not valid!", "OK");
                return;
            }
            outputLabel.Text = await Task.Run(() => IPv6Helpers.Compress(ref address, (byte)address.Length));
        }
    }
}
