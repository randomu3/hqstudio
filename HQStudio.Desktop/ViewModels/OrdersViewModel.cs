using HQStudio.Models;
using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class OrdersViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;
        private Order? _selectedOrder;

        public ObservableCollection<Order> Orders { get; } = new();

        public Order? SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }

        public ICommand AddOrderCommand { get; }
        public ICommand EditOrderCommand { get; }
        public ICommand CompleteOrderCommand { get; }
        public ICommand DeleteOrderCommand { get; }
        public ICommand RefreshCommand { get; }

        public OrdersViewModel()
        {
            AddOrderCommand = new RelayCommand(_ => AddOrder());
            EditOrderCommand = new RelayCommand(_ => EditOrder(), _ => SelectedOrder != null);
            CompleteOrderCommand = new RelayCommand(_ => CompleteOrder(), _ => SelectedOrder != null && SelectedOrder.Status != "Завершен");
            DeleteOrderCommand = new RelayCommand(_ => DeleteOrderAsync(), _ => SelectedOrder != null);
            RefreshCommand = new RelayCommand(async _ => await LoadOrdersAsync());
            _ = LoadOrdersAsync();
        }

        public async Task LoadOrdersAsync()
        {
            Orders.Clear();
            
            if (_settings.UseApi && _apiService.IsConnected)
            {
                var apiOrders = await _apiService.GetOrdersAsync();
                foreach (var apiOrder in apiOrders.OrderByDescending(o => o.CreatedAt))
                {
                    Orders.Add(new Order
                    {
                        Id = apiOrder.Id,
                        ClientId = apiOrder.ClientId,
                        ClientName = apiOrder.Client?.Name ?? "Неизвестный",
                        Status = MapStatus(apiOrder.Status),
                        TotalPrice = apiOrder.TotalPrice,
                        Notes = apiOrder.Notes,
                        CreatedAt = apiOrder.CreatedAt,
                        CompletedAt = apiOrder.CompletedAt
                    });
                }
            }
            else
            {
                foreach (var order in _dataService.Orders.OrderByDescending(o => o.CreatedAt))
                {
                    Orders.Add(order);
                }
            }
        }

        public void LoadOrders()
        {
            _ = LoadOrdersAsync();
        }

        private string MapStatus(string apiStatus)
        {
            return apiStatus switch
            {
                "New" => "Новый",
                "InProgress" => "В работе",
                "Completed" => "Завершен",
                "Cancelled" => "Отменен",
                _ => apiStatus
            };
        }

        private void AddOrder()
        {
            if (!_dataService.Clients.Any())
            {
                MessageBox.Show("Сначала добавьте клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new EditOrderDialog();
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                dialog.Order.Id = _dataService.GetNextId(_dataService.Orders);
                dialog.Order.CreatedAt = DateTime.Now;
                _dataService.Orders.Add(dialog.Order);
                _dataService.SaveData();
                LoadOrders();
            }
        }

        public void EditOrder(Order? order = null)
        {
            var orderToEdit = order ?? SelectedOrder;
            if (orderToEdit == null) return;
            
            var dialog = new EditOrderDialog(orderToEdit);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                _dataService.SaveData();
                LoadOrders();
            }
        }

        private async void CompleteOrder()
        {
            if (SelectedOrder == null) return;
            
            if (_settings.UseApi && _apiService.IsConnected)
            {
                await _apiService.UpdateOrderStatusAsync(SelectedOrder.Id, "Completed");
            }
            else
            {
                SelectedOrder.Status = "Завершен";
                SelectedOrder.CompletedAt = DateTime.Now;
                _dataService.SaveData();
            }
            
            await LoadOrdersAsync();
        }

        private async void DeleteOrderAsync()
        {
            if (SelectedOrder == null) return;
            
            var result = MessageBox.Show(
                $"Удалить заказ #{SelectedOrder.Id}?\n\nЗаказ будет помечен как удалённый, но сохранится в базе данных.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                if (_settings.UseApi && _apiService.IsConnected)
                {
                    var success = await _apiService.DeleteOrderAsync(SelectedOrder.Id);
                    if (success)
                    {
                        await LoadOrdersAsync();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить заказ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    _dataService.Orders.Remove(SelectedOrder);
                    _dataService.SaveData();
                    LoadOrders();
                }
            }
        }
    }
}
