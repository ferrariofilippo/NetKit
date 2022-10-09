using NetKit.Services;
using NetKit.ViewModels;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CompressPage : ContentPage
    {
        private readonly CompressViewModel viewModel;
        private string[] address;

        public CompressPage()
        {
            InitializeComponent();
            viewModel = new CompressViewModel();
            BindingContext = viewModel;
        }

        private async void CompressClicked(object sender, EventArgs e)
        {
            if (!IPv6Helpers.ValidateAddress(viewModel.IpAddress, out address))
            {
                await DisplayAlert("Error", "IP address entered is not valid!", "OK");
                return;
            }
            viewModel.OutputAddress = await Task.Run(() => IPv6Helpers.Compress(ref address, (byte)address.Length));
        }
    }
}
