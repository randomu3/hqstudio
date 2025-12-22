using HQStudio.Services;
using HQStudio.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.Views.Dialogs
{
    public partial class CreateClientFromCallbackDialog : Window
    {
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly CallbackItem _callback;
        private ApiClient? _existingClient;
        private List<ApiClient> _allClients = new();
        private System.Timers.Timer? _searchTimer;

        public ApiClient? CreatedClient { get; private set; }
        public bool LinkedToExisting { get; private set; }
        public ApiClient? LinkedClient => _existingClient;
        public bool CreateOrderAfterClient { get; private set; }

        public CreateClientFromCallbackDialog(CallbackItem callback)
        {
            InitializeComponent();
            _callback = callback;
            
            LoadCallbackData();
            _ = LoadClientsAndCheckDuplicates();
            
            Loaded += (s, e) => NameBox.Focus();
        }

        private void LoadCallbackData()
        {
            NameBox.Text = _callback.Name;
            PhoneBox.Text = _callback.Phone;
            CarBox.Text = _callback.CarModel ?? "";
            CarNumberBox.Text = _callback.LicensePlate ?? "";
            NotesBox.Text = $"Создан из заявки #{_callback.Id}";
            
            SourceIcon.Text = _callback.SourceIcon;
            SourceText.Text = _callback.Source;
            CallbackDateText.Text = $"Заявка от {_callback.FormattedDate}";
            
            if (!string.IsNullOrWhiteSpace(_callback.Message))
            {
                NotesBox.Text += $"\n\nСообщение: {_callback.Message}";
            }
        }

        private async Task LoadClientsAndCheckDuplicates()
        {
            _allClients = await _apiService.GetClientsAsync();
            CheckForDuplicates();
        }

        private void Phone_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Debounce поиск дубликатов
            _searchTimer?.Stop();
            _searchTimer?.Dispose();
            _searchTimer = new System.Timers.Timer(300);
            _searchTimer.Elapsed += (s, args) =>
            {
                _searchTimer?.Stop();
                Dispatcher.Invoke(CheckForDuplicates);
            };
            _searchTimer.Start();
        }

        private void CheckForDuplicates()
        {
            var phone = NormalizePhone(PhoneBox.Text);
            
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 5)
            {
                DuplicateWarning.Visibility = Visibility.Collapsed;
                _existingClient = null;
                return;
            }

            // Ищем по телефону
            _existingClient = _allClients.FirstOrDefault(c => 
                NormalizePhone(c.Phone) == phone ||
                NormalizePhone(c.Phone).Contains(phone) ||
                phone.Contains(NormalizePhone(c.Phone)));

            if (_existingClient != null)
            {
                ExistingClientName.Text = _existingClient.Name;
                ExistingClientPhone.Text = _existingClient.Phone;
                ExistingClientCar.Text = !string.IsNullOrWhiteSpace(_existingClient.CarModel) 
                    ? $"{_existingClient.CarModel} • {_existingClient.LicensePlate ?? "без номера"}"
                    : "Автомобиль не указан";
                DuplicateWarning.Visibility = Visibility.Visible;
            }
            else
            {
                DuplicateWarning.Visibility = Visibility.Collapsed;
            }
        }

        private static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";
            return new string(phone.Where(char.IsDigit).ToArray());
        }

        private void LinkToExisting_Click(object sender, RoutedEventArgs e)
        {
            if (_existingClient == null) return;

            var confirmed = ConfirmDialog.Show(
                "Привязка к клиенту",
                $"Привязать заявку #{_callback.Id} к клиенту {_existingClient.Name}?\n\nЗаявка будет добавлена в историю этого клиента.",
                ConfirmDialog.DialogType.Question,
                "Привязать", "Отмена", this);

            if (confirmed)
            {
                LinkedToExisting = true;
                DialogResult = true;
                Close();
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                ConfirmDialog.ShowInfo("Ошибка", "Введите имя клиента", ConfirmDialog.DialogType.Warning, this);
                NameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PhoneBox.Text))
            {
                ConfirmDialog.ShowInfo("Ошибка", "Введите телефон клиента", ConfirmDialog.DialogType.Warning, this);
                PhoneBox.Focus();
                return;
            }

            // Если есть дубликат, предупреждаем
            if (_existingClient != null)
            {
                var confirmed = ConfirmDialog.Show(
                    "Возможный дубликат",
                    $"Клиент с похожим телефоном уже существует:\n{_existingClient.Name} ({_existingClient.Phone})\n\nВсё равно создать нового клиента?",
                    ConfirmDialog.DialogType.Warning,
                    "Создать", "Отмена", this);

                if (!confirmed)
                    return;
            }

            SaveBtn.IsEnabled = false;
            SaveBtn.Content = "Создание...";

            try
            {
                var client = new ApiClient
                {
                    Name = NameBox.Text.Trim(),
                    Phone = PhoneBox.Text.Trim(),
                    CarModel = string.IsNullOrWhiteSpace(CarBox.Text) ? null : CarBox.Text.Trim(),
                    LicensePlate = string.IsNullOrWhiteSpace(CarNumberBox.Text) ? null : CarNumberBox.Text.Trim().ToUpper(),
                    Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim()
                };

                var (createdClient, error) = await _apiService.CreateClientAsync(client);
                CreatedClient = createdClient;

                if (CreatedClient != null)
                {
                    // Логируем действие
                    await _apiService.LogActivityAsync(
                        "Создан клиент из заявки",
                        "Client",
                        CreatedClient.Id,
                        $"Клиент {CreatedClient.Name} создан из заявки #{_callback.Id}");

                    // Спрашиваем о создании заказа
                    CreateOrderAfterClient = ConfirmDialog.Show(
                        "Клиент создан",
                        $"Клиент {CreatedClient.Name} успешно создан!\n\nСоздать заказ для этого клиента?",
                        ConfirmDialog.DialogType.Success,
                        "Создать заказ", "Позже", this);
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ConfirmDialog.ShowInfo("Ошибка", error ?? "Не удалось создать клиента", 
                        ConfirmDialog.DialogType.Error, this);
                }
            }
            catch (Exception ex)
            {
                ConfirmDialog.ShowInfo("Ошибка", $"Ошибка: {ex.Message}", ConfirmDialog.DialogType.Error, this);
            }
            finally
            {
                SaveBtn.IsEnabled = true;
                SaveBtn.Content = "✓ Создать клиента";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Cancel_Click(sender, e);

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1) DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Cancel_Click(sender, e);
            }
        }
    }
}
