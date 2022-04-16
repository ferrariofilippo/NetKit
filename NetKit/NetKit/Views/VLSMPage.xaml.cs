using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using NetKit.Model;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class VLSMPage : ContentPage
    {
        readonly ObservableCollection<HostLine> addedItems = new ObservableCollection<HostLine>();
        readonly ObservableCollection<Network> networks = new ObservableCollection<Network>();
        readonly List<Network> reti = new List<Network>();
        int height = 30;

        readonly byte[] lastUsed = { 10, 0, 0, 0 };
        readonly List<uint> maxValues = new List<uint>();
        List<uint> values;
        int len = 0;

        public VLSMPage()
        {
            InitializeComponent();
            Get_MaxValues();
            hostsListView.ItemsSource = addedItems;
            outputListView.ItemsSource = networks;
            outputListView.HeightRequest = 10;
            Add_Clicked(null, EventArgs.Empty);
        }

        /// <summary>
        /// Bottone + Premuto
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Add_Clicked(object sender, EventArgs e)
        {
            // Aggiungi un elemento e adatta la dimensione della ListView
            addedItems.Add(new HostLine());
            height += hostsListView.RowHeight;
            hostsListView.HeightRequest = height;
        }

        /// <summary>
        /// Bottone Elimina premuto
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Delete_Clicked(object sender, EventArgs e)
        {
            int lastIndex = addedItems.Count - 1;
            if (lastIndex > 0)
            {
                // Rimuovi l'ultimo elemento e adatta la dimensione della ListView
                addedItems.RemoveAt(lastIndex);
                height -= hostsListView.RowHeight;
                hostsListView.HeightRequest = height;
            }
        }

        async void GetResults_Clicked(object sender, EventArgs e)
        {
            vlsmButton.IsEnabled = false;
            await Task.Run(() =>
            {
                try
                {
                    values = Get_Values();
                    len = values.Count;
                    Get_Prefixes();
                    Get_Subnets();
                    Get_Networks();
                    Get_Broadcast();
                    outputListView.HeightRequest = 30 + outputListView.RowHeight * reti.Count;
                    networks.Clear();
                    foreach (var item in reti)
                    {
                        networks.Add(item);
                    }
                    reti.Clear();
                    for (int i = 0; i < 4; i++)
                    {
                        lastUsed[i] = 0;
                    }
                }
                catch (Exception)
                {
                    DisplayAlert("Errore", "I dati inseriti non sono nel formato corretto!", "OK");
                }
            });
            vlsmButton.IsEnabled = true;
        }

        /// <summary>
        /// Metodo per il parsing dei dati in input
        /// </summary>
        /// <returns>Lista con le dimensioni delle subnet</returns>
        List<uint> Get_Values()
        {
            List<uint> data = new List<uint>();
            foreach (var item in addedItems)
            {
                if (item.Data.StartsWith("/") || item.Data.StartsWith("\\"))    // Calcola Rete dato il Prefix Length
                    data.Add(GetSize(item.Data));
                else
                    data.Add(uint.Parse(item.Data));    // Calcola Rete dato il numero di host
            }
            return data;
        }

        /// <summary>
        /// Calcola la dimensione di una rete dato il prefix length
        /// </summary>
        /// <param name="data">Prefix Length</param>
        /// <returns>Numero di host massimi nella rete</returns>
        /// <exception cref="Exception">Prefix Length Non Valido</exception>
        uint GetSize(string data)
        {
            data = data.Substring(1);   // Rimuovi '/' o '\'
            byte size = byte.Parse(data);
            size = size % 8 != 0 ? size : (byte)(size - 1);
            sbyte cycles = (sbyte)(30 - size);  // Calcola l'esponente
            if (cycles >= 0)
                return maxValues[cycles];
            throw new Exception("Prefix Length non valido");
        }

        /// <summary>
        /// Calcola una potenza di 2 (Fino a 2^32)
        /// </summary>
        /// <param name="y">Esponente</param>
        /// <returns>2^y</returns>
        public static uint PowerOfTwo(int y)
        {
            uint temp = 1;
            return temp << y;
        }

        void Get_Prefixes()
        {
            for (int i = 0; i < len; i++)
            {
                byte j = 0;
                while (values[i] > maxValues[j])
                    j++;

                values[i] = maxValues[j];
            }
            values.Sort();
            values.Reverse();

            for (int i = 0; i < len; i++)
            {
                byte prefix = 0;
                while (maxValues[prefix] < values[i])
                    prefix++;

                prefix = (byte)(30 - prefix);
                reti.Add(new Network()
                {
                    PrefixLength = prefix % 8 != 0 ? prefix : (byte)(prefix + 1),
                    NumeroHost = values[i]
                });
            }
        }

        void Get_Subnets()
        {
            byte[] mask;
            for (int i = 0; i < len; i++)
            {
                mask = Subnet(reti[i].PrefixLength);
                reti[i].SubnetMask = $"{mask[0]}.{mask[1]}.{mask[2]}.{mask[3]}";
            }
        }

        public static byte[] Subnet(byte prefix)
        {
            byte[] mask = new byte[4];
            for (int j = 0; j < 4; j++)
            {
                for (int k = 0; k < 8; k++)
                {
                    if (j * 8 + k < prefix)
                        mask[j] += (byte)PowerOfTwo(7 - k);
                    else
                    {
                        k = 9;
                        j = 5;
                    }
                }
            }
            return mask;
        }

        void Get_Networks()
        {
            if (reti[0].PrefixLength > 24)
            {
                lastUsed[0] = 192;
                lastUsed[1] = 168;
            }
            else if (reti[0].PrefixLength > 16)
            {
                lastUsed[0] = 172;
                lastUsed[1] = 16;
            }
            else if (reti[0].PrefixLength <= 8)
                lastUsed[0] = 0;
            for (int i = 0; i < len; i++)
            {
                reti[i].NetworkAddress = $"{lastUsed[0]}.{lastUsed[1]}.{lastUsed[2]}.{lastUsed[3]}";
                if (reti[i].PrefixLength > 24)
                    lastUsed[3] += (byte)PowerOfTwo(32 - reti[i].PrefixLength);
                else if (reti[i].PrefixLength > 16)
                    lastUsed[2] += (byte)PowerOfTwo(24 - reti[i].PrefixLength);
                else if (reti[i].PrefixLength > 8)
                    lastUsed[1] += (byte)PowerOfTwo(16 - reti[i].PrefixLength);
                else
                    lastUsed[0] += (byte)PowerOfTwo(8 - reti[i].PrefixLength);
            }
        }

        void Get_Broadcast()
        {
            byte[] ip = new byte[4];
            for (int i = 0; i < len; i++)
            {
                byte hostBits = (byte)(32 - reti[i].PrefixLength);
                byte cont = 0;
                foreach (var item in reti[i].NetworkAddress.Split('.'))
                {
                    ip[cont] = byte.Parse(item);
                    cont++;
                }
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        if (j * 8 + k < hostBits)
                            ip[3 - j] += (byte)PowerOfTwo(k);
                        else
                        {
                            k = 9;
                            j = 5;
                        }
                    }
                }
                reti[i].BroadcastAddress = $"{ip[0]}.{ip[1]}.{ip[2]}.{ip[3]}";
            }
        }

        /// <summary>
        /// Carica la lista con il numero massimo di host per ogni dimensione della rete
        /// </summary>
        void Get_MaxValues()
        {
            maxValues.Add(4);
            for (int i = 0; i < 30; i++)
            {
                maxValues.Add(maxValues[i] << 1);
            }
            for (int i = 0; i < maxValues.Count; i++)
            {
                maxValues[i] -= 2;
            }
        }
    }
}