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

        private void SubmitButton_Clicked(object sender, EventArgs e)
        {
            var networkAddress = new byte[4];
            if (ValidateInput(networkAddress))
                _viewModel.CalculateACEs(networkAddress);
        }

        private bool ValidateInput(byte[] address)
        {
            if (!IPv4Helpers.TryParseAddress(_viewModel.NetworkAddress, address))
            {
                DisplayAlert("Invalid Data", "Given network address is invalid", "OK");
                return false;
            }
            if (_viewModel.IsRange)
            {
                if (string.IsNullOrWhiteSpace(_viewModel.LowerBound) || !int.TryParse(_viewModel.LowerBound, out _))
                {
                    DisplayAlert("Invalid Data", "Lower bound is invalid", "OK");
                    return false;
                }
                else if (string.IsNullOrWhiteSpace(_viewModel.UpperBound) || !int.TryParse(_viewModel.UpperBound, out _))
                {
                    DisplayAlert("Invalid Data", "Upper bound is invalid", "OK");
                    return false;
                }
            }
            else if (_viewModel.IsGreaterOrSmaller && !int.TryParse(_viewModel.ValueLimit, out _))
            {
                DisplayAlert("Invalid Data", "Limit value is invalid", "OK");
                return false;
            }
            return true;
        }
    }
}