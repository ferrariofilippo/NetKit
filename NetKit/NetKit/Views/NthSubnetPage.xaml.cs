using NetKit.Services;
using NetKit.ViewModels;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NthSubnetPage : ContentPage
    {
        private readonly NthSubnetViewModel viewModel;
        private readonly byte[] subnet = new byte[4];
        private readonly byte[] address = { 0, 0, 0, 0 };
        private byte value;

        public NthSubnetPage()
        {
            InitializeComponent();
            viewModel = new NthSubnetViewModel();
            BindingContext = viewModel;
        }

        private async void GetResults_Clicked(object sender, EventArgs e)
        {
            if (viewModel.SubnetMask == null || !IPv4Helpers.TryParseAddress(viewModel.SubnetMask, subnet)) 
            {
                await DisplayAlert("Error", "Entered Subnet Mask is not valid!", "OK");
                return;
            }
            else if (viewModel.SubnetNumber == null || !await Task.Run(() => IsValueValid()))
            {
                await DisplayAlert("Error", "Entered number is not valid for this Subnet Mask!", "OK");
                return;
            }
            viewModel.NetworkAddress = String.Format(
                "Subnet Address: {0}.{1}.{2}.{3}",
                address[0],
                address[1],
                address[2],
                address[3]);
        }

        private bool IsValueValid()
        {
            if (!byte.TryParse(viewModel.SubnetNumber, out value) || value <= 0)
                return false;

            for (int i = 0; i < 4; i++)
            {
                if (subnet[i] != 255)
                {
                    switch (i)
                    {
                        case 1:
                            address[0] = 10;
                            break;
                        case 2:
                            address[0] = 172;
                            address[1] = 16;
                            break;
                        case 3: 
                            address[0] = 192;
                            address[1] = 168;
                            break;
                    }
                    byte lastBitOne = (byte)(7 - Convert.ToString(subnet[i], 2).LastIndexOf('1'));
                    if (value > MathHelpers.PowerOfTwo(8 - lastBitOne))
                        return false;

                    byte magicNumber = (byte)(MathHelpers.PowerOfTwo(lastBitOne) * (value - 1));
                    address[i] = magicNumber;
                    return true;
                }
            }
            return false;
        }
    }
}
