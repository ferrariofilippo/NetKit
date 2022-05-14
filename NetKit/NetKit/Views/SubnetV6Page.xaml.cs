using NetKit.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SubnetV6Page : ContentPage
    {
        readonly string[] baseAddress = new string[8];
        byte globalRoutingPrefix = 0;
        byte subnetId = 0;
        ushort howMany = 0;

        readonly List<string> addresses = new List<string>();
        readonly DataList outAddresses = new DataList();

        readonly SemaphoreSlim semaphore = new SemaphoreSlim(0);
        Task processData = new Task(() => { });
        CancellationTokenSource tokenSource = new CancellationTokenSource();
        CancellationToken token;

        public SubnetV6Page()
        {
            InitializeComponent();
            token = tokenSource.Token;

            Binding listViewBinding = new Binding("Data", BindingMode.TwoWay, source: outAddresses);
            subnetListView.SetBinding(ListView.ItemsSourceProperty, listViewBinding);
        }

        void CalcolaClicked(object sender, EventArgs e)
        {
            try
            {
                // Read data from User Input
                GetBaseAddress();
                if (globalPrefixEntry.Text == null)
                {
                    throw new FormatException("Global Routing Prefix is null");
                }

                globalRoutingPrefix = byte.Parse(globalPrefixEntry.Text);
                subnetId = (byte)(16 - (globalRoutingPrefix % 16));

                if (howManyEntry.Text == null)
                {
                    throw new FormatException("Number not valid");
                }

                howMany = ushort.Parse(howManyEntry.Text);
                byte index = (byte)((globalRoutingPrefix + subnetId) / 16 - 1);
                string lastDigit = $"{baseAddress[index][baseAddress.Length - 1]}";
                byte baseValue = byte.Parse(lastDigit, 
                    System.Globalization.NumberStyles.HexNumber);

                if (howMany > (ushort)(1 << subnetId - baseValue))
                {
                    throw new FormatException("Subnet Id is too small for this many subnets");
                }
                else if (howMany > short.MaxValue)
                {
                    DisplayAlert("Alert", $"You are trying to get {howMany} " +
                        $"subnets, it will take a while", "I'll wait");
                }

                Submit(howMany);
            }
            catch(FormatException fe)
            {
                DisplayAlert("Error", fe.Message, "OK");
            }
            catch (Exception)
            {
                DisplayAlert("Error", "Entered data is not valid", "OK");
            }
        }

        void GetBaseAddress()
        {
            if (baseAddressEntry.Text == null)
            {
                throw new FormatException("Base address in null");
            }

            foreach (var c in baseAddressEntry.Text.ToUpper())
            {
                if ((c < 48 || c > 57) && (c < 65 || c > 90) && c != ':')
                {
                    throw new FormatException("Base Address in not hex");
                }
            }

            string[] address;
            baseAddressEntry.Text = baseAddressEntry.Text.Trim();
            if (baseAddressEntry.Text.EndsWith("::"))
            {
                address = baseAddressEntry.Text.Remove(baseAddressEntry.Text.LastIndexOf(":")).Split(':');
            }
            else
            {
                address = baseAddressEntry.Text.Split(':');
                if (address.Length != 8)
                    throw new FormatException("Base address in not in the correct format");
            }

            byte len = (byte)address.Length;
            for (byte i = 0, j = 0; i < len; i++)
            {
                if (address[i] != "")
                {
                    baseAddress[j] = address[i];
                    j++;
                }
                else
                {
                    byte n = (byte)(9 - len);
                    for (byte k = 0; k < n; k++)
                    {
                        baseAddress[j + k] = "0";
                    }
                    j += n;
                }
            }
        }

        string GetFormattedAddress(uint value, byte index)
        {
            string[] temp = new string[8];
            for (byte i = 0; i < 8; i++)
            {
                if (i != index)
                {
                    temp[i] = baseAddress[i];
                }
                else
                {
                    temp[i] = Convert.ToString(value, 16);
                }
            }

            return CompressPage.Comprimi(temp, 8, new bool[8]);
        }

        async Task Submit(uint howMany)
        {
            try
            {
                if (processData.Status == TaskStatus.Running)
                {
                    tokenSource.Cancel();
                    await Task.Run(() => semaphore.Wait()); // Attendi che il task in esecuzione termini
                }

                processData = Task.Run(() =>
                {
                    // Get Subnets
                    byte index = (byte)((globalRoutingPrefix + subnetId) / 16 - 1);

                    ushort address = ushort.Parse(baseAddress[index], 
                        System.Globalization.NumberStyles.HexNumber);
                    addresses.Clear();
                    outAddresses.Data = addresses;
                    for (ushort i = 0; i < howMany; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        addresses.Add(GetFormattedAddress(address, index));
                        address++;
                    }
                    token.ThrowIfCancellationRequested();
                    outAddresses.Data = addresses;
                    ForceLayout();
                }, token);
                await processData;
            }
            catch (Exception)
            {
                tokenSource = new CancellationTokenSource();
                token = tokenSource.Token;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}