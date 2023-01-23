using NetKit.Services;
using NetKit.ViewModels;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WildcardPage : ContentPage
    {
        private readonly WildcardViewModel _viewModel = new WildcardViewModel();

        public WildcardPage()
        {
            InitializeComponent();
            BindingContext = _viewModel;
            MethodPicker.ItemsSource = _viewModel.WildcardMethods;
            ACEListView.ItemsSource = _viewModel.AccessControlEntries;
        }

        private void MethodEntry_Focused(object sender, FocusEventArgs e)
        {
            MethodPicker.Focus();
        }
        private void MethodPicker_Unfocused(object sender, FocusEventArgs e)
        {
            MethodEntry.Unfocus();
        }
        private void MethodPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            MethodEntry.Unfocus();
            _viewModel.WildcardMethod = (WildcardMethod)MethodPicker.SelectedIndex;
            _viewModel.MethodName = _viewModel.WildcardMethods[MethodPicker.SelectedIndex];
        }

        private async void SubmitButton_Clicked(object sender, EventArgs e)
        {
            var networkAddress = new byte[4];
            if (ValidateInput(networkAddress, out int networkBits))
                await _viewModel.CalculateACEs(networkAddress, networkBits);
        }

        private bool ValidateInput(byte[] address, out int networkBits)
        {
            networkBits = 0;
            var addressComponents = _viewModel.NetworkAddress.Split('/', '\\');
            if (addressComponents.Length != 2 || !int.TryParse(addressComponents[1], out networkBits) || networkBits > 30)
            {
                DisplayAlert("Invalid Data", "Network's /nn is invalid", "OK");
                return false;
            }
            if (!IPv4Helpers.TryParseAddress(addressComponents[0], address))
            {
                DisplayAlert("Invalid Data", "Given network address is invalid", "OK");
                return false;
            }
            if (_viewModel.IsRange)
            {
                if (string.IsNullOrWhiteSpace(_viewModel.LowerBound) || !uint.TryParse(_viewModel.LowerBound, out uint lower))
                {
                    DisplayAlert("Invalid Data", "Lower bound is invalid", "OK");
                    return false;
                }
                else if (string.IsNullOrWhiteSpace(_viewModel.UpperBound) || !uint.TryParse(_viewModel.UpperBound, out uint upper) || lower > upper)
                {
                    DisplayAlert("Invalid Data", "Upper bound is invalid", "OK");
                    return false;
                }
            }
            else if (_viewModel.IsGreaterOrSmaller && !uint.TryParse(_viewModel.ValueLimit, out _))
            {
                DisplayAlert("Invalid Data", "Limit value is invalid", "OK");
                return false;
            }
            return true;
        }
    }
}