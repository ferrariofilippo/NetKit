using System;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CompressPage : ContentPage
    {
        readonly bool[] isSegmentO = new bool[8];
        byte len = 0;
        string[] address;

        public CompressPage()
        {
            InitializeComponent();
        }

        async void CalcolaClicked(object sender, EventArgs e)
        {
            compressButton.IsEnabled = false;
            await Task.Run(() =>
            {
                if (!IsAddressValid())
                {
                    DisplayAlert("Errore", "L'indirizzo inserito non è valido", "OK");
                    return;
                }
                outputLabel.Text = Comprimi(address, len, isSegmentO);
            });
            compressButton.IsEnabled = true;
        }

        static string OmitLeading(string segment, byte index, bool[] isSegmentO)
        {
            if (!segment[0].Equals('0'))
                return segment;
            if (segment.Length == 1)
            {
                if (segment[0].Equals('0'))
                    isSegmentO[index] = true;
                return segment;
            }
            return OmitLeading(segment.Substring(1), index, isSegmentO);
        }

        public static string Comprimi(string[] address, byte len, bool[] isSegmentO)
        {
            byte[] param = new byte[2];
            StringBuilder output = new StringBuilder();
            for (byte i = 0; i < len; i++)
            {
                if (!address[i].Equals(""))
                    address[i] = OmitLeading(address[i], i, isSegmentO);
                else
                    address[i] = "0";
            }
            for (byte i = 0; i < len; i++)
            {
                byte temp = i;
                byte size = 0;
                while (i < len && address[i].Equals("0"))
                {
                    i++;
                    size++;
                }
                if (size > param[1])
                {
                    param[0] = temp;
                    param[1] = size;
                }
            }

            for (byte i = 0; i < param[1]; i++)
            {
                address[i + param[0]] = "";
            }

            for (byte i = 0; i < len; i++)
            {
                if (address[i].Equals(""))
                {
                    if (i == 0)
                        output.Append("::");
                    else
                        output.Append(":");
                    if (param[1] != 0)
                        i += (byte)(param[1] - 1);
                }
                else
                {
                    output.Append(address[i]);
                    if (i != 7)
                        output.Append(":");
                }
            }
            return output.ToString();
        }

        bool IsAddressValid()
        {
            if (ipEntry.Text == null)
                return false;
            string value = ipEntry.Text.ToUpper();
            if (!IsHex(value))
                return false;
            address = value.Split(':');
            len = (byte)address.Length;
            if (len != 8)
                return false;
            return true;
        }

        static bool IsHex(string value)
        {
            foreach (var c in value)
            {
                if ((c < 48 || c > 57) && (c < 65 || c > 70) && c != 58)
                    return false;
            }
            return true;
        }
    }
}