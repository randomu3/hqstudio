using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HQStudio.Services
{
    /// <summary>
    /// Сервис уведомлений о новых заявках и заказах
    /// </summary>
    public class NotificationService
    {
        private static NotificationService? _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        private CancellationTokenSource? _pollCts;
        private int _lastCallbackId;
        private int _lastOrderId;
        private bool _isPolling;

        public event Action<string, string>? OnNewCallback;
        public event Action<string, string>? OnNewOrder;

        private NotificationService() { }

        /// <summary>
        /// Запустить polling для новых заявок/заказов
        /// </summary>
        public void StartPolling(int intervalSeconds = 30)
        {
            if (_isPolling) return;
            _isPolling = true;

            _pollCts = new CancellationTokenSource();
            _ = PollForUpdatesAsync(intervalSeconds, _pollCts.Token);
        }

        /// <summary>
        /// Остановить polling
        /// </summary>
        public void StopPolling()
        {
            _isPolling = false;
            _pollCts?.Cancel();
            _pollCts?.Dispose();
            _pollCts = null;
        }

        private async Task PollForUpdatesAsync(int intervalSeconds, CancellationToken ct)
        {
            // Инициализируем последние ID
            await InitializeLastIdsAsync();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);
                    await CheckForNewCallbacksAsync();
                    await CheckForNewOrdersAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Notification polling error: {ex.Message}");
                }
            }
        }

        private async Task InitializeLastIdsAsync()
        {
            try
            {
                var callbacks = await ApiService.Instance.GetCallbacksAsync();
                if (callbacks?.Any() == true)
                    _lastCallbackId = callbacks.Max(c => c.Id);

                var ordersResponse = await ApiService.Instance.GetOrdersAsync();
                if (ordersResponse?.Items?.Any() == true)
                    _lastOrderId = ordersResponse.Items.Max(o => o.Id);
            }
            catch { }
        }

        private async Task CheckForNewCallbacksAsync()
        {
            try
            {
                var callbacks = await ApiService.Instance.GetCallbacksAsync();
                if (callbacks == null) return;

                var newCallbacks = callbacks.Where(c => c.Id > _lastCallbackId).ToList();
                foreach (var callback in newCallbacks)
                {
                    OnNewCallback?.Invoke(callback.Name, callback.Phone);
                    
                    // Показываем MessageBox в UI потоке
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"Новая заявка от {callback.Name}\nТелефон: {callback.Phone}",
                            "📞 Новая заявка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                }

                if (newCallbacks.Any())
                    _lastCallbackId = newCallbacks.Max(c => c.Id);
            }
            catch { }
        }

        private async Task CheckForNewOrdersAsync()
        {
            try
            {
                var ordersResponse = await ApiService.Instance.GetOrdersAsync();
                if (ordersResponse?.Items == null) return;

                var newOrders = ordersResponse.Items.Where(o => o.Id > _lastOrderId).ToList();
                foreach (var order in newOrders)
                {
                    OnNewOrder?.Invoke(order.Client?.Name ?? "Новый заказ", $"#{order.Id}");
                    
                    // Показываем MessageBox в UI потоке
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"Новый заказ #{order.Id}\nКлиент: {order.Client?.Name ?? "—"}",
                            "📋 Новый заказ",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                }

                if (newOrders.Any())
                    _lastOrderId = newOrders.Max(o => o.Id);
            }
            catch { }
        }
    }
}
