using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using HQStudio.Services;
using HQStudio.ViewModels;

namespace HQStudio.Views.Dialogs
{
    public partial class CallbackDetailsDialog : Window
    {
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly CallbackItem _callback;
        private ApiClient? _existingClient;
        
        public bool CreateClientRequested { get; private set; }
        public bool CreateOrderRequested { get; private set; }
        public ApiClient? ExistingClient => _existingClient;

        public CallbackDetailsDialog(CallbackItem callback)
        {
            InitializeComponent();
            _callback = callback;
            DataContext = new CallbackDetailsViewModel(callback);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckExistingClient();
        }

        private async Task CheckExistingClient()
        {
            try
            {
                var clients = await _apiService.GetClientsAsync();
                var phone = NormalizePhone(_callback.Phone);
                
                _existingClient = clients.FirstOrDefault(c => 
                    NormalizePhone(c.Phone) == phone ||
                    NormalizePhone(c.Phone).Contains(phone) ||
                    phone.Contains(NormalizePhone(c.Phone)));

                if (_existingClient != null)
                {
                    // Клиент найден - показываем инфо и кнопку создания заказа
                    ExistingClientName.Text = _existingClient.Name;
                    ExistingClientInfo.Text = !string.IsNullOrWhiteSpace(_existingClient.CarModel)
                        ? $"{_existingClient.CarModel} • {_existingClient.LicensePlate ?? "без номера"}"
                        : _existingClient.Phone;
                    
                    ExistingClientPanel.Visibility = Visibility.Visible;
                    CreateClientBtn.Visibility = Visibility.Collapsed;
                    CreateOrderBtn.Visibility = Visibility.Visible;
                }
                else
                {
                    // Клиент не найден - показываем кнопку создания клиента
                    ExistingClientPanel.Visibility = Visibility.Collapsed;
                    CreateClientBtn.Visibility = Visibility.Visible;
                    CreateOrderBtn.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                // При ошибке показываем обе кнопки
                CreateClientBtn.Visibility = Visibility.Visible;
                CreateOrderBtn.Visibility = Visibility.Collapsed;
            }
        }

        private static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";
            return new string(phone.Where(char.IsDigit).ToArray());
        }

        private void CreateClient_Click(object sender, RoutedEventArgs e)
        {
            CreateClientRequested = true;
            DialogResult = true;
            Close();
        }

        private void CreateOrder_Click(object sender, RoutedEventArgs e)
        {
            CreateOrderRequested = true;
            DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }

    public class CallbackDetailsViewModel
    {
        private readonly CallbackItem _callback;

        public CallbackDetailsViewModel(CallbackItem callback)
        {
            _callback = callback;
        }

        public string Title => $"Заявка #{_callback.Id}";
        public string Name => _callback.Name;
        public string Phone => _callback.Phone;
        public string? CarModel => _callback.CarModel;
        public string? LicensePlate => _callback.LicensePlate;
        public string? Message => _callback.Message;
        public string Status => _callback.Status;
        public string Source => _callback.Source;
        public string SourceIcon => _callback.SourceIcon;
        public string FormattedDate => _callback.FormattedDate;
        public bool HasMessage => !string.IsNullOrWhiteSpace(_callback.Message);
        
        public Brush StatusBgColor => _callback.Status switch
        {
            "Новая" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2A1A")),
            "В работе" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2A3A")),
            "Завершена" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2A1A")),
            "Отменена" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A1A1A")),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A"))
        };
        
        public Brush StatusFgColor => _callback.Status switch
        {
            "Новая" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107")),
            "В работе" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
            "Завершена" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
            "Отменена" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#707070"))
        };
    }
}
