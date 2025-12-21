using HQStudio.Services;
using System.Windows;

namespace HQStudio
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize theme
            ThemeService.Instance.Initialize();
            
            // Start session for online status tracking
            _ = SessionService.Instance.StartSessionAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // End session when app closes
            SessionService.Instance.EndSessionAsync().Wait();
            base.OnExit(e);
        }
    }
}
