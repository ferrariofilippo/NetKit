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
        private const int BITS_PER_BYTE = 8;
        private const int BYTES_PER_ADDRESS = 4;
        private const int BYTE_MAX_VALUE = 255;

        private readonly NthSubnetViewModel viewModel;
        private readonly byte[] subnet = new byte[BYTES_PER_ADDRESS];
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

            for (int i = 0; i < BYTES_PER_ADDRESS; i++)
            {
                if (subnet[i] != BYTE_MAX_VALUE)
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
                    var lastBitOne = (byte)(7 - Convert.ToString(subnet[i], 2).LastIndexOf('1'));
                    if (value > MathHelpers.PowersOfTwo[BITS_PER_BYTE - lastBitOne])
                        return false;

                    var magicNumber = (byte)(MathHelpers.PowersOfTwo[lastBitOne] * (value - 1));
                    address[i] = magicNumber;
                    return true;
                }
            }
            return false;
        }
    }
}
