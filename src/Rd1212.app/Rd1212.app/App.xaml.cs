using CodeBrix.Prism;
using Prism;
using Prism.DryIoc;
using Prism.Ioc;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Rd1212.app
{
    public partial class App : PrismApplication
    {
        public App() : this(null) { }  //This constructor should never be called at run-time

        public App(IPlatformInitializer initializer) : base(initializer) { }

        protected override void OnInitialized()
        {
            InitializeComponent();
            NavigationService.NavigateAsync($"{nameof(NavigationPage)}/{nameof(Views.MainPage)}");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //Register my views and services here
            containerRegistry.RegisterForNavigation<Views.MainPage>();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
            CodeBrixInfo.OnApplicationStart();
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            CodeBrixInfo.OnApplicationSleep();
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
            CodeBrixInfo.OnApplicationResume();
        }
    }
}
