using HQStudio.Services;
using HQStudio.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                InitializeNotifications();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации MainWindow: {ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeNotifications()
        {
            try
            {
                // Запускаем polling для уведомлений
                var notifications = NotificationService.Instance;
                
                notifications.OnNewCallback += (name, phone) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (DataContext is MainViewModel vm)
                        {
                            vm.HasNewNotifications = true;
                        }
                    });
                };

                notifications.OnNewOrder += (client, orderId) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (DataContext is MainViewModel vm)
                        {
                            vm.HasNewNotifications = true;
                        }
                    });
                };

                // Запускаем polling (каждые 30 секунд)
                notifications.StartPolling(30);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeNotifications error: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            NotificationService.Instance.StopPolling();
            base.OnClosed(e);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeRestore();
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            MaximizeRestore();
        }

        private void MaximizeRestore()
        {
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            // Навигация по разделам через Alt + цифры
            if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                switch (e.Key)
                {
                    case Key.D1:
                        vm.NavigateCommand.Execute("Dashboard");
                        e.Handled = true;
                        break;
                    case Key.D2:
                        vm.NavigateCommand.Execute("Services");
                        e.Handled = true;
                        break;
                    case Key.D3:
                        vm.NavigateCommand.Execute("Clients");
                        e.Handled = true;
                        break;
                    case Key.D4:
                        vm.NavigateCommand.Execute("Orders");
                        e.Handled = true;
                        break;
                    case Key.D5:
                        vm.NavigateCommand.Execute("Staff");
                        e.Handled = true;
                        break;
                    case Key.D6:
                        vm.NavigateCommand.Execute("Settings");
                        e.Handled = true;
                        break;
                }
            }
            
            // F5 - обновить данные
            if (e.Key == Key.F5)
            {
                vm.RefreshCommand?.Execute(null);
                e.Handled = true;
            }
        }
    }
}
