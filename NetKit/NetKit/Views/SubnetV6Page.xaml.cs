using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using NetKit.Model;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SubnetV6Page : ContentPage
    {
        readonly string[] baseAddress = new string[8];
        byte globalRoutingPrefix = 0;
        byte subnetId = 0;
        uint howMany = 0;

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
                    throw new FormatException("Global Routing Prefix is null");
                globalRoutingPrefix = byte.Parse(globalPrefixEntry.Text);

                if (subnetEntry.Text == null)
                    throw new FormatException("Subnet ID is null");
                subnetId = byte.Parse(subnetEntry.Text);

                if (howManyEntry.Text == null)
                    throw new FormatException("Number not valid");
                howMany = uint.Parse(howManyEntry.Text);

                if (howMany > 16_777_216)
                    throw new FormatException("Number too big");
                else if (howMany > (uint)(1 << (subnetId - 1)))
                    throw new FormatException("Number too big");
                else if (howMany > 100_000)
                    DisplayAlert("Alert", $"You are trying to get {howMany} subnets, it will take a while", "I'll wait");

                Submit(howMany);
            }
            catch (Exception)
            {
                DisplayAlert("Errore", "Dati inseriti non validi", "OK");
            }
        }

        void GetBaseAddress()
        {
            if (baseAddressEntry.Text == null)
                throw new FormatException("Base address in null");

            foreach (var c in baseAddressEntry.Text.ToUpper())
            {
                if ((c < 48 || c > 57) && (c < 65 || c > 90) && c != ':')
                    throw new FormatException("Base Address in not hex");
            }

            string[] address;
            baseAddressEntry.Text = baseAddressEntry.Text.Trim();
            if (baseAddressEntry.Text.EndsWith("::"))
            {
                address = baseAddressEntry.Text.Remove(baseAddressEntry.Text.LastIndexOf(":")).Split(':');
            }
            else
                address = baseAddressEntry.Text.Split(':');
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
                    temp[i] = baseAddress[i];
                else
                    temp[i] = Convert.ToString(value, 16);
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
                    byte[] indexes = {
                        (byte)((globalRoutingPrefix + subnetId) / 16 - 1),
                        (byte)(subnetId % 16 == 0 ? 0 : 16 - (subnetId % 16))
                    };

                    ushort address = ushort.Parse(baseAddress[indexes[0]], System.Globalization.NumberStyles.HexNumber);
                    bool flag = true;
                    ushort increment = (ushort)(1 << indexes[1]);
                    addresses.Clear();
                    for (uint i = 0; i < howMany; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        if (i > ushort.MaxValue && flag)
                        {
                            indexes[0]--;
                            indexes[1] = (byte)(subnetId - indexes[1]);
                            increment = (ushort)(1 << indexes[1]);
                            address = ushort.Parse(baseAddress[indexes[0]], System.Globalization.NumberStyles.HexNumber);
                            flag = false;
                        }
                        addresses.Add(GetFormattedAddress(address, indexes[0]));
                        address += increment;
                    }
                    token.ThrowIfCancellationRequested();
                    outAddresses.Data = addresses;
                }, token);
                await processData;
            }
            catch (Exception exc)
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