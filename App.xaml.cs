using Microsoft.Extensions.DependencyInjection;
using WashTrack.MVVM.Views;

namespace WashTrack
{
    public partial class App : Application
    {
        private readonly IServiceProvider _services;

        public App(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
        }

        // Resolved here rather than via constructor injection: LoginPage's
        // XAML uses StaticResource lookups against Application.Current.Resources,
        // which isn't populated until InitializeComponent() above runs. Building
        // LoginPage as a constructor parameter would construct it too early.
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var loginPage = _services.GetRequiredService<LoginPage>();
            return new Window(loginPage);
        }
    }
}