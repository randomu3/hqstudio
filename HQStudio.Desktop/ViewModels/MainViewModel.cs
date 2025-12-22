using HQStudio.Services;
using System.Reflection;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private BaseViewModel? _currentView;
        private string _currentViewName = "Dashboard";
        private int? _pendingOrderId;

        public BaseViewModel? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string CurrentViewName
        {
            get => _currentViewName;
            set => SetProperty(ref _currentViewName, value);
        }

        public string UserDisplayName => _dataService.CurrentUser?.DisplayName ?? "Гость";
        public string UserRole => _dataService.CurrentUser?.Role ?? "";
        
        public string AppVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version != null ? $"v{version.Major}.{version.Minor}" : "v1.0";
            }
        }

        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand? RefreshCommand { get; }

        public MainViewModel()
        {
            NavigateCommand = new RelayCommand(Navigate);
            LogoutCommand = new RelayCommand(_ => Logout());
            RefreshCommand = new RelayCommand(_ => Refresh());
            CurrentView = new DashboardViewModel();
        }

        private void Navigate(object? parameter)
        {
            if (parameter is not string viewName) return;

            CurrentViewName = viewName;
            CurrentView = viewName switch
            {
                "Dashboard" => new DashboardViewModel(),
                "Services" => new ServicesViewModel(),
                "Clients" => new ClientsViewModel(),
                "Orders" => CreateOrdersViewModel(),
                "Callbacks" => CreateCallbacksViewModel(),
                "Staff" => new StaffViewModel(),
                "ActivityLog" => new ActivityLogViewModel(),
                "Settings" => new SettingsViewModel(),
                _ => CurrentView
            };
        }

        private OrdersViewModel CreateOrdersViewModel()
        {
            var vm = new OrdersViewModel();
            
            // Если есть отложенный заказ для выделения
            if (_pendingOrderId.HasValue)
            {
                var orderId = _pendingOrderId.Value;
                _pendingOrderId = null;
                
                // Выделяем заказ после загрузки
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(OrdersViewModel.IsLoading) && !vm.IsLoading)
                    {
                        vm.SelectedOrder = vm.Orders.FirstOrDefault(o => o.Id == orderId);
                    }
                };
            }
            
            return vm;
        }

        private CallbacksViewModel CreateCallbacksViewModel()
        {
            var vm = new CallbacksViewModel();
            vm.NavigateToOrder += OnNavigateToOrder;
            return vm;
        }

        private void OnNavigateToOrder(int orderId)
        {
            _pendingOrderId = orderId;
            Navigate("Orders");
        }

        public void NavigateToOrder(int orderId)
        {
            _pendingOrderId = orderId;
            Navigate("Orders");
        }

        private void Refresh()
        {
            // Перезагружаем текущее представление
            Navigate(CurrentViewName);
        }

        private void Logout()
        {
            _dataService.Logout();
        }
    }
}
