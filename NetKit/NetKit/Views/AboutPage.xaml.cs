using NetKit.ViewModels;
using System.IO;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

namespace NetKit.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : ContentPage
    {
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
            using (StreamReader stream = new StreamReader(await FileSystem.OpenAppPackageFileAsync("About.txt")))
            {
                viewModel.Description = stream.ReadToEnd();
            }        
        }

        private async void LinkTapped(object sender, System.EventArgs e)
        {
            await Launcher.OpenAsync(new System.Uri("https://icons8.com"));
        }
    }
}
