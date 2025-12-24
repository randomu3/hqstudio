using HQStudio.Models;
using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class CallbacksViewModel : BaseViewModel
    {
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;

        public ObservableCollection<CallbackItem> Callbacks { get; } = new();
        public ObservableCollection<string> StatusFilters { get; } = new() { "Все", "Новые", "В работе", "Завершённые", "Отменённые" };
        public ObservableCollection<string> SourceFilters { get; } = new() { "Все", "Сайт", "Звонок", "Живой приход", "Почта", "Мессенджер" };

        private CallbackItem? _selectedCallback;
        public CallbackItem? SelectedCallback
        {
            get => _selectedCallback;
            set { SetProperty(ref _selectedCallback, value); OnPropertyChanged(nameof(HasSelection)); }
        }

        private string _selectedStatus = "Все";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set { SetProperty(ref _selectedStatus, value); }
        }

        private string _selectedSource = "Все";
        public string SelectedSource
        {
            get => _selectedSource;
            set { SetProperty(ref _selectedSource, value); }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); FilterCallbacks(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isApiConnected;
        public bool IsApiConnected
        {
            get => _isApiConnected;
            set => SetProperty(ref _isApiConnected, value);
        }

        private CallbackStats? _stats;
        public CallbackStats? Stats
        {
            get => _stats;
            set => SetProperty(ref _stats, value);
        }

        public bool HasSelection => SelectedCallback != null;

        private List<CallbackItem> _allCallbacks = new();

        public ICommand RefreshCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand TakeInWorkCommand { get; }
        public ICommand CompleteCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CreateClientCommand { get; }
        public ICommand CreateOrderCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand OpenCallbackCommand { get; }

        public CallbacksViewModel()
        {
            RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
            ApplyFiltersCommand = new RelayCommand(_ => FilterCallbacks());
            TakeInWorkCommand = new RelayCommand(async _ => await ChangeStatusAsync("Processing"), _ => SelectedCallback?.Status == "Новая");
            CompleteCommand = new RelayCommand(async _ => await ChangeStatusAsync("Completed"), _ => SelectedCallback?.Status == "В работе");
            CancelCommand = new RelayCommand(async _ => await ChangeStatusAsync("Cancelled"), _ => SelectedCallback != null && SelectedCallback.Status != "Завершена" && SelectedCallback.Status != "Отменена");
            CreateClientCommand = new RelayCommand(async _ => await CreateClientFromCallbackAsync(), _ => SelectedCallback != null);
            CreateOrderCommand = new RelayCommand(_ => CreateOrderFromCallback(), _ => SelectedCallback != null);
            DeleteCommand = new RelayCommand(async _ => await DeleteCallbackAsync(), _ => SelectedCallback != null);
            OpenCallbackCommand = new RelayCommand(_ => OpenCallback(), _ => SelectedCallback != null);

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            System.Diagnostics.Debug.WriteLine("=== LoadDataAsync started ===");
            
            // Проверяем подключение к API
            if (_settings.UseApi)
            {
                System.Diagnostics.Debug.WriteLine($"UseApi=true, checking connection to {_settings.ApiUrl}");
                var connected = await _apiService.CheckConnectionAsync();
                IsApiConnected = connected;
                System.Diagnostics.Debug.WriteLine($"Connection result: {connected}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("UseApi=false");
                IsApiConnected = false;
            }

            if (!IsApiConnected)
            {
                IsLoading = false;
                if (_settings.UseApi)
                {
                    ConfirmDialog.ShowInfo(
                        "Нет подключения к API",
                        $"Не удалось подключиться к серверу.\nПроверьте что сервер запущен на {_settings.ApiUrl}",
                        ConfirmDialog.DialogType.Warning);
                }
                return;
            }

            try
            {
                // Загружаем статистику
                System.Diagnostics.Debug.WriteLine("Loading stats...");
                var stats = await _apiService.GetCallbackStatsAsync();
                if (stats != null)
                {
                    Stats = stats;
                    System.Diagnostics.Debug.WriteLine($"Stats loaded: New={stats.TotalNew}, Processing={stats.TotalProcessing}, Completed={stats.TotalCompleted}");
                }

                // Загружаем заявки
                System.Diagnostics.Debug.WriteLine("Loading callbacks...");
                var callbacks = await _apiService.GetCallbacksAsync();
                System.Diagnostics.Debug.WriteLine($"Callbacks loaded: {callbacks.Count}");
                
                _allCallbacks = callbacks.Select(c => new CallbackItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    CarModel = c.CarModel,
                    LicensePlate = c.LicensePlate,
                    Message = c.Message,
                    Status = MapStatus(c.Status),
                    Source = MapSource(c.Source),
                    SourceDetails = c.SourceDetails,
                    CreatedAt = c.CreatedAt,
                    ProcessedAt = c.ProcessedAt,
                    CompletedAt = c.CompletedAt
                }).ToList();

                FilterCallbacks();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadDataAsync error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ConfirmDialog.ShowInfo("Ошибка загрузки", ex.Message, ConfirmDialog.DialogType.Error);
            }

            IsLoading = false;
        }

        private void FilterCallbacks()
        {
            Callbacks.Clear();

            var filtered = _allCallbacks.AsEnumerable();

            // Фильтр по статусу
            if (SelectedStatus != "Все")
            {
                filtered = filtered.Where(c => c.Status == SelectedStatus.Replace("ые", "ая").Replace("ённые", "ена").Replace("ённые", "ена"));
            }

            // Фильтр по источнику
            if (SelectedSource != "Все")
            {
                filtered = filtered.Where(c => c.Source == SelectedSource);
            }

            // Поиск
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(c =>
                    c.Name.ToLower().Contains(search) ||
                    c.Phone.Contains(search) ||
                    (c.CarModel?.ToLower().Contains(search) ?? false) ||
                    (c.LicensePlate?.ToLower().Contains(search) ?? false));
            }

            foreach (var callback in filtered.OrderByDescending(c => c.CreatedAt))
            {
                Callbacks.Add(callback);
            }
        }

        private async Task ChangeStatusAsync(string newStatus)
        {
            if (SelectedCallback == null) return;

            var success = await _apiService.UpdateCallbackStatusAsync(SelectedCallback.Id, newStatus);
            if (success)
            {
                await LoadDataAsync();
            }
        }

        private async Task CreateClientFromCallbackAsync()
        {
            if (SelectedCallback == null) return;

            var dialog = new CreateClientFromCallbackDialog(SelectedCallback)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                ApiClient? clientForOrder = null;
                
                if (dialog.LinkedToExisting && dialog.LinkedClient != null)
                {
                    // Привязали к существующему клиенту
                    clientForOrder = dialog.LinkedClient;
                    
                    // Обновляем статус заявки на "В работе"
                    await _apiService.UpdateCallbackStatusAsync(SelectedCallback.Id, "Processing");
                    
                    // Спрашиваем о создании заказа
                    var createOrder = ConfirmDialog.Show(
                        "Заявка привязана",
                        $"Заявка привязана к клиенту {dialog.LinkedClient.Name}.\n\nСоздать заказ для этого клиента?",
                        ConfirmDialog.DialogType.Question,
                        "Создать заказ", "Нет");
                    
                    if (createOrder)
                    {
                        OpenOrderDialogForClient(clientForOrder);
                    }
                }
                else if (dialog.CreatedClient != null)
                {
                    // Создали нового клиента
                    clientForOrder = dialog.CreatedClient;
                    
                    // Обновляем статус заявки на "В работе"
                    await _apiService.UpdateCallbackStatusAsync(SelectedCallback.Id, "Processing");
                    
                    // Если пользователь хочет создать заказ
                    if (dialog.CreateOrderAfterClient)
                    {
                        OpenOrderDialogForClient(clientForOrder);
                    }
                }

                await LoadDataAsync();
            }
        }

        // Событие для навигации к заказам
        public event Action<int>? NavigateToOrder;

        private async void OpenOrderDialogForClient(ApiClient client)
        {
            // Конвертируем ApiClient в локальную модель Client для диалога
            var localClient = new Client
            {
                Id = client.Id,
                Name = client.Name,
                Phone = client.Phone,
                Car = client.CarModel ?? "",
                CarNumber = client.LicensePlate ?? "",
                Notes = client.Notes ?? ""
            };
            
            // Добавляем клиента в DataService если его там нет
            var dataService = DataService.Instance;
            if (!dataService.Clients.Any(c => c.Id == client.Id))
            {
                dataService.Clients.Add(localClient);
            }
            
            // Создаём новый заказ с предустановленным клиентом
            var order = new Order
            {
                ClientId = client.Id,
                Client = localClient,
                Status = "Новый",
                Notes = SelectedCallback != null 
                    ? $"Создан из заявки #{SelectedCallback.Id} ({SelectedCallback.Source})"
                    : ""
            };
            
            var orderDialog = new EditOrderDialog(order)
            {
                Owner = Application.Current.MainWindow
            };
            
            if (orderDialog.ShowDialog() == true)
            {
                int? createdOrderId = null;
                
                // Сохраняем заказ через API если подключены
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var request = new CreateOrderRequest
                    {
                        ClientId = orderDialog.Order.ClientId,
                        ServiceIds = orderDialog.Order.ServiceIds,
                        TotalPrice = orderDialog.Order.TotalPrice,
                        Notes = orderDialog.Order.Notes
                    };
                    
                    var created = await _apiService.CreateOrderAsync(request);
                    if (created != null)
                    {
                        createdOrderId = created.Id;
                    }
                }
                else
                {
                    var savedOrder = orderDialog.Order;
                    savedOrder.Id = dataService.GetNextId(dataService.Orders);
                    savedOrder.CreatedAt = DateTime.Now;
                    dataService.Orders.Add(savedOrder);
                    dataService.SaveData();
                    createdOrderId = savedOrder.Id;
                }
                
                if (createdOrderId.HasValue)
                {
                    var goToOrder = ConfirmDialog.Show(
                        "Заказ создан",
                        $"Заказ #{createdOrderId} успешно создан!\n\nПерейти к заказу?",
                        ConfirmDialog.DialogType.Success,
                        "Перейти к заказу", "Остаться здесь");
                    
                    if (goToOrder)
                    {
                        NavigateToOrder?.Invoke(createdOrderId.Value);
                    }
                }
                else
                {
                    ConfirmDialog.ShowInfo("Ошибка", "Не удалось создать заказ", ConfirmDialog.DialogType.Error);
                }
            }
        }

        private async void CreateOrderFromCallback()
        {
            if (SelectedCallback == null) return;
            
            // Ищем клиента по телефону
            var clients = await _apiService.GetClientsAsync();
            var phone = NormalizePhone(SelectedCallback.Phone);
            
            var existingClient = clients.FirstOrDefault(c => 
                NormalizePhone(c.Phone) == phone ||
                NormalizePhone(c.Phone).Contains(phone) ||
                phone.Contains(NormalizePhone(c.Phone)));

            if (existingClient != null)
            {
                OpenOrderDialogForClient(existingClient);
            }
            else
            {
                ConfirmDialog.ShowInfo(
                    "Клиент не найден",
                    "Сначала создайте клиента из этой заявки.\n\nНажмите кнопку \"Создать клиента\".",
                    ConfirmDialog.DialogType.Warning);
            }
        }

        private static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";
            return new string(phone.Where(char.IsDigit).ToArray());
        }

        private async void OpenCallback()
        {
            if (SelectedCallback == null) return;
            
            var dialog = new CallbackDetailsDialog(SelectedCallback)
            {
                Owner = Application.Current.MainWindow
            };
            
            if (dialog.ShowDialog() == true)
            {
                if (dialog.CreateClientRequested)
                {
                    // Открываем диалог создания клиента
                    await CreateClientFromCallbackAsync();
                }
                else if (dialog.CreateOrderRequested && dialog.ExistingClient != null)
                {
                    // Открываем диалог создания заказа с существующим клиентом
                    OpenOrderDialogForClient(dialog.ExistingClient);
                    await _apiService.UpdateCallbackStatusAsync(SelectedCallback.Id, "Processing");
                    await LoadDataAsync();
                }
            }
        }

        private async Task DeleteCallbackAsync()
        {
            if (SelectedCallback == null) return;

            var result = ConfirmDialog.Show(
                "Подтверждение удаления",
                $"Удалить заявку #{SelectedCallback.Id} от {SelectedCallback.Name}?",
                ConfirmDialog.DialogType.Warning,
                "Удалить", "Отмена");

            if (result)
            {
                var success = await _apiService.DeleteCallbackAsync(SelectedCallback.Id);
                if (success)
                {
                    await LoadDataAsync();
                }
            }
        }

        private string MapStatus(int apiStatus)
        {
            return apiStatus switch
            {
                0 => "Новая",
                1 => "В работе",
                2 => "Завершена",
                3 => "Отменена",
                _ => $"Статус {apiStatus}"
            };
        }

        private string MapSource(int source)
        {
            return source switch
            {
                0 => "Сайт",
                1 => "Звонок",
                2 => "Живой приход",
                3 => "Почта",
                4 => "Мессенджер",
                5 => "Рекомендация",
                6 => "Другое",
                _ => "Неизвестно"
            };
        }
    }

    public class CallbackItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Message { get; set; }
        public string Status { get; set; } = "";
        public string Source { get; set; } = "";
        public string? SourceDetails { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public string FormattedDate => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        public string StatusColor => Status switch
        {
            "Новая" => "#FFC107",
            "В работе" => "#2196F3",
            "Завершена" => "#4CAF50",
            "Отменена" => "#F44336",
            _ => "#707070"
        };
        public string SourceIcon => Source switch
        {
            "Сайт" => "🌐",
            "Звонок" => "📞",
            "Живой приход" => "🚶",
            "Почта" => "📧",
            "Мессенджер" => "💬",
            "Рекомендация" => "👥",
            _ => "❓"
        };
    }
}
