using HQStudio.Services;
using HQStudio.Views;
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
            
            // Screenshot mode - пропускаем авторизацию и делаем скриншоты
            if (ScreenshotService.IsScreenshotMode)
            {
                _ = RunScreenshotModeAsync();
                return;
            }
            
            // Start data sync service if API is enabled
            if (SettingsService.Instance.UseApi)
            {
                _ = InitializeApiAsync();
            }
            
            // Session is started after login in LoginViewModel
        }

        private async Task InitializeApiAsync()
        {
            // Проверяем подключение к API
            var connected = await ApiService.Instance.CheckConnectionAsync();
            if (connected)
            {
                // Запускаем автосинхронизацию
                DataSyncService.Instance.Start();
            }
        }

        private async Task RunScreenshotModeAsync()
        {
            Console.WriteLine($"Screenshot mode: hidden={ScreenshotService.IsHiddenMode}");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // 1. Скриншот окна входа
            var loginWindow = new LoginWindow();
            await ScreenshotService.ShowAndWaitAsync(loginWindow, 500);
            ScreenshotService.CaptureWindow(loginWindow, "01-login.png");
            
            // 2. Открываем главное окно
            var mainWindow = new MainWindow();
            await ScreenshotService.ShowAndWaitAsync(mainWindow, 800);
            loginWindow.Close();
            
            // 3. Скриншот Dashboard
            ScreenshotService.CaptureWindow(mainWindow, "02-dashboard.png");
            
            // 4. Навигация по разделам
            if (mainWindow.DataContext is ViewModels.MainViewModel vm)
            {
                var screens = new[] {
                    ("Orders", "03-orders.png"),
                    ("Clients", "04-clients.png"),
                    ("Services", "05-services.png"),
                    ("Staff", "06-staff.png"),
                    ("Settings", "07-settings.png")
                };
                
                foreach (var (view, filename) in screens)
                {
                    vm.NavigateCommand.Execute(view);
                    await Task.Delay(400);
                    ScreenshotService.CaptureWindow(mainWindow, filename);
                }
                
                // Светлая тема
                ThemeService.Instance.ApplyTheme(false);
                vm.NavigateCommand.Execute("Dashboard");
                await Task.Delay(300);
                ScreenshotService.CaptureWindow(mainWindow, "08-dashboard-light.png");
            }
            
            sw.Stop();
            Console.WriteLine($"Screenshots completed in {sw.ElapsedMilliseconds}ms");
            ScreenshotService.ExitAfterDelay(200);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Stop data sync
            DataSyncService.Instance.Stop();
            
            // End session when app closes
            SessionService.Instance.EndSessionAsync().Wait();
            base.OnExit(e);
        }
    }
}
