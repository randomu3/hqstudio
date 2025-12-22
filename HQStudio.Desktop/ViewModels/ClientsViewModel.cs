using HQStudio.Models;
using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class ClientsViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;
        
        private Client? _selectedClient;
        private string _searchText = string.Empty;
        private List<Client> _allClients = new();

        public ObservableCollection<Client> Clients { get; } = new();

        public Client? SelectedClient
        {
            get => _selectedClient;
            set => SetProperty(ref _selectedClient, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterClients();
            }
        }

        public ICommand AddClientCommand { get; }
        public ICommand EditClientCommand { get; }
        public ICommand DeleteClientCommand { get; }
        public ICommand RefreshCommand { get; }

        public ClientsViewModel()
        {
            AddClientCommand = new RelayCommand(_ => AddClientAsync());
            EditClientCommand = new RelayCommand(_ => EditClient(), _ => SelectedClient != null);
            DeleteClientCommand = new RelayCommand(_ => DeleteClient(), _ => SelectedClient != null);
            RefreshCommand = new RelayCommand(async _ => await LoadClientsAsync());
            _ = LoadClientsAsync();
        }

        private async Task LoadClientsAsync()
        {
            _allClients.Clear();
            
            if (_settings.UseApi && _apiService.IsConnected)
            {
                var apiClients = await _apiService.GetClientsAsync();
                _allClients = apiClients.Select(c => new Client
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    Car = c.CarModel ?? "",
                    CarNumber = c.LicensePlate ?? "",
                    Notes = c.Notes ?? "",
                    CreatedAt = c.CreatedAt
                }).ToList();
            }
            else
            {
                _allClients = _dataService.Clients.ToList();
            }
            
            FilterClients();
        }

        private void FilterClients()
        {
            Clients.Clear();
            
            var filtered = string.IsNullOrEmpty(SearchText)
                ? _allClients
                : _allClients.Where(c =>
                    c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Phone.Replace(" ", "").Replace("-", "").Contains(SearchText.Replace(" ", "").Replace("-", ""), StringComparison.OrdinalIgnoreCase) ||
                    c.Car.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    c.CarNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var client in filtered.OrderByDescending(c => c.CreatedAt))
            {
                Clients.Add(client);
            }
        }

        private async void AddClientAsync()
        {
            var dialog = new EditClientDialog();
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                // Проверка на дубликат локально
                var normalizedPhone = dialog.Client.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
                var existingClient = _allClients.FirstOrDefault(c => 
                    c.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") == normalizedPhone);
                
                if (existingClient != null)
                {
                    MessageBox.Show(
                        $"Клиент с таким номером телефона уже существует:\n{existingClient.Name}\n\nИспользуйте существующего клиента для создания заказа.",
                        "Дубликат клиента",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var apiClient = new ApiClient
                    {
                        Name = dialog.Client.Name,
                        Phone = dialog.Client.Phone,
                        CarModel = dialog.Client.Car,
                        LicensePlate = dialog.Client.CarNumber,
                        Notes = dialog.Client.Notes
                    };
                    
                    var (created, error) = await _apiService.CreateClientAsync(apiClient);
                    if (created == null)
                    {
                        MessageBox.Show(error ?? "Не удалось создать клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    dialog.Client.Id = _dataService.GetNextId(_dataService.Clients);
                    dialog.Client.CreatedAt = DateTime.Now;
                    _dataService.Clients.Add(dialog.Client);
                    _dataService.SaveData();
                }
                
                await LoadClientsAsync();
            }
        }

        private void EditClient()
        {
            if (SelectedClient == null) return;
            
            var dialog = new EditClientDialog(SelectedClient);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                _dataService.SaveData();
                _ = LoadClientsAsync();
            }
        }

        private void DeleteClient()
        {
            if (SelectedClient == null) return;
            
            var result = MessageBox.Show(
                $"Удалить клиента \"{SelectedClient.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _dataService.Clients.Remove(SelectedClient);
                _dataService.SaveData();
                _ = LoadClientsAsync();
            }
        }
    }
}
