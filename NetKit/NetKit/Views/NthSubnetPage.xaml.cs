using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NthSubnetPage : ContentPage
    {
        byte value;
        readonly byte[] subnet = new byte[4];
        readonly byte[] address = { 0, 0, 0, 0 };

        public NthSubnetPage()
        {
            InitializeComponent();
        }

        async void GetResults_Clicked(object sender, EventArgs e)
        {
            if (maskEntry.Text == null || !IsSubnetValid())
            {
                await DisplayAlert("Errore", "La subnet mask inserita non è valida", "OK");
                return;
            }
            else if (valueEntry.Text == null || !await Task.Run(() => IsValueValid()))
            {
                await DisplayAlert("Errore", "Il numero inserito non è valido", "OK");
                return;
            }
            outputLabel.Text = $"Indirizzo sottorete: {address[0]}.{address[1]}.{address[2]}.{address[3]}";
        }

        bool IsSubnetValid()
        {
            string[] fields = maskEntry.Text.Split('.');
            int len = fields.Length;
            if (len != 4)
            {
                return false;
            }

            try
            {
                for (int i = 0; i < len; i++)
                {
                    subnet[i] = byte.Parse(fields[i]);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        bool IsValueValid()
        {
            if (!byte.TryParse(valueEntry.Text, out value) || value <= 0)
            {
                return false;
            }

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
                    if (value > VLSMPage.PowerOfTwo(8 - lastOne))
                    {
                        return false;
                    }

                    byte magicNumber = (byte)(VLSMPage.PowerOfTwo(lastOne) * (value - 1));
                    address[i] = magicNumber;
                    return true;
                }
            }
            return false;
        }
    }
}