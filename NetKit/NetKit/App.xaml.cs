using NetKit.Services;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace NetKit
{
    public partial class App : Application
    {

        public App()
        {
            MathHelpers.Init();
            IPv4Helpers.Init();

            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override void OnStart() { }

        protected override void OnSleep() { }

        protected override void OnResume() { }
    }
}
