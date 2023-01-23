using NetKit.ViewModels;
using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : ContentPage
    {
        private const string ABOUT_PATH = "About.txt";

        private readonly Uri _iconsCredits = new Uri("https://icons8.com");

        private readonly AboutViewModel viewModel;

        public AboutPage()
        {
            InitializeComponent();
            viewModel = new AboutViewModel();
            BindingContext = viewModel;
            ReadData();
        }

        private async void ReadData()
        {
            using (var stream = new StreamReader(await FileSystem.OpenAppPackageFileAsync(ABOUT_PATH)))
                viewModel.Description = stream.ReadToEnd();
        }

        private async void LinkTapped(object sender, EventArgs e)
        {
            await Launcher.OpenAsync(_iconsCredits);
        }
    }
}
