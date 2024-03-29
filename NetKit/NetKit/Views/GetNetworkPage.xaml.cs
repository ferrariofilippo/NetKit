﻿using NetKit.Services;
using NetKit.ViewModels;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GetNetworkPage : ContentPage
    {
        private const int BYTES_PER_ADDRESS = 4;

        private readonly GetNetworkViewModel viewModel;
        private readonly byte[] network = new byte[BYTES_PER_ADDRESS];
        private readonly byte[] host = new byte[BYTES_PER_ADDRESS];
        private readonly byte[] mask = new byte[BYTES_PER_ADDRESS];
        private byte prefixLength;

        public GetNetworkPage()
        {
            InitializeComponent();
            viewModel = new GetNetworkViewModel();
            BindingContext = viewModel;
        }

        private async void Submit(object sender, EventArgs e)
        {
            if (!await Task.Run(() => IPv4Helpers.TryParseAddress(viewModel.HostNumber, host)))
            {
                await DisplayAlert("Error", "Host Address is not valid!", "OK");
                return;
            }
            if (!await Task.Run(() => GetNetworkAddress()))
            {
                await DisplayAlert("Error", "Subnet Mask is not valid!", "OK");
                return;
            }

            viewModel.NetworkAddress = string.Format(
                "Network Address: {0}.{1}.{2}.{3}",
                network[0],
                network[1],
                network[2],
                network[3]);
        }

        private bool GetNetworkAddress()
        {
            string value = viewModel.SubnetMask;
            if (
                string.IsNullOrWhiteSpace(value) ||
                (
                (value.StartsWith("\\") || value.StartsWith("/")) && 
                (!byte.TryParse(value.Substring(1), out prefixLength) || !IPv4Helpers.TryGetSubnetMask(prefixLength, mask))
                ) ||
                !IPv4Helpers.TryParseAddress(value, mask)
                )
            {
                return false;
            }

            for (int i = 0; i < BYTES_PER_ADDRESS; i++)
                network[i] = (byte)(host[i] & mask[i]);
            return true;
        }
    }
}
