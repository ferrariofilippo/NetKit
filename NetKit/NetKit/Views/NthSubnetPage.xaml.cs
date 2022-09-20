using NetKit.Services;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NthSubnetPage : ContentPage
    {
        private byte value;
        private readonly byte[] subnet = new byte[4];
        private readonly byte[] address = { 0, 0, 0, 0 };

        public NthSubnetPage()
        {
            InitializeComponent();
        }

        private async void GetResults_Clicked(object sender, EventArgs e)
        {
            if (maskEntry.Text == null || !IPv4Helpers.ValidateSubnetMask(subnet, maskEntry.Text.Split('.')))
            {
                await DisplayAlert("Error", "Entered Subnet Mask is not valid!", "OK");
                return;
            }
            else if (valueEntry.Text == null || !await Task.Run(() => IsValueValid()))
            {
                await DisplayAlert("Error", "Entered number is not valid for this Subnet Mask!", "OK");
                return;
            }
            outputLabel.Text = $"Subnet Address: {address[0]}.{address[1]}.{address[2]}.{address[3]}";
        }

        private bool IsValueValid()
        {
            if (!byte.TryParse(valueEntry.Text, out value) || value <= 0)
                return false;

            for (int i = 0; i < 4; i++)
            {
                if (subnet[i] != 255)
                {
                    if (i == 1)
                    {
                        address[0] = 10;
                    }
                    else if (i == 2)
                    {
                        address[0] = 172;
                        address[1] = 16;
                    }
                    else if (i == 3)
                    {
                        address[0] = 192;
                        address[1] = 168;
                    }
                    byte lastOne = (byte)(7 - Convert.ToString(subnet[i], 2).LastIndexOf('1'));
                    if (value > MathHelpers.PowerOfTwo(8 - lastOne))
                        return false;

                    byte magicNumber = (byte)(MathHelpers.PowerOfTwo(lastOne) * (value - 1));
                    address[i] = magicNumber;
                    return true;
                }
            }
            return false;
        }
    }
}
