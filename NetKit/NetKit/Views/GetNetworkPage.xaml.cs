using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GetNetworkPage : ContentPage
    {
        readonly byte[] network = new byte[4];
        readonly byte[] host = new byte[4];
        byte[] mask = new byte[4];
        byte prefixLength;

        public GetNetworkPage()
        {
            InitializeComponent();
        }

        async void Calcola(object sender, EventArgs e)
        {
            if (!await Task.Run(() => GetHost()))
            {
                await DisplayAlert("Errore", "L'indirizzo host inserito non è valido", "OK");
                return;
            }
            if (!await Task.Run(() => Get_Network()))
            {
                await DisplayAlert("Errore", "La mask inserita non è valida", "OK");
                return;
            }
            outputLabel.Text = $"Indirizzo di Rete: {network[0]}.{network[1]}" +
                $".{network[2]}.{network[3]}";
        }

        bool GetHost()
        {
            string value = hostEntry.Text;
            if (value == null)
            {
                return false;
            }

            string[] fields = value.Split('.');
            if (fields.Length != 4)
            {
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                if (!byte.TryParse(fields[i], out host[i]))
                {
                    return false;
                }
            }
            return true;
        }

        bool Get_Network()
        {
            string value = maskEntry.Text;
            if (value == null)
            {
                return false;
            }

            if (value.StartsWith("\\") || value.StartsWith("/"))
            {
                if (!byte.TryParse(value.Substring(1), out prefixLength))
                {
                    return false;
                }

                mask = VLSMPage.Subnet(prefixLength);
            }
            else
            {
                string[] fields = value.Split('.');
                if (fields.Length != 4)
                {
                    return false;
                }

                for (int i = 0; i < 4; i++)
                {
                    if (!byte.TryParse(fields[i], out mask[i]))
                    {
                        return false;
                    }
                }
            }

            for (int i = 0; i < 4; i++)
            {
                network[i] = (byte)(host[i] & mask[i]);
            }
            return true;
        }
    }
}