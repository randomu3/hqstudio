using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;

namespace HQStudio.Services
{
    /// <summary>
    /// Сервис Windows Toast уведомлений
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
        public event Action<int, string>? OnOrderStatusChanged;

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
                    ShowNewCallbackNotification(callback.Name, callback.Phone);
                    OnNewCallback?.Invoke(callback.Name, callback.Phone);
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
                    ShowNewOrderNotification(order.Id, order.Client?.Name ?? "Клиент");
                    OnNewOrder?.Invoke(order.Client?.Name ?? "Новый заказ", $"#{order.Id}");
                }

                if (newOrders.Any())
                    _lastOrderId = newOrders.Max(o => o.Id);
            }
            catch { }
        }

        /// <summary>
        /// Показать уведомление о новой заявке
        /// </summary>
        public void ShowNewCallbackNotification(string clientName, string phone)
        {
            ShowToast(
                "📞 Новая заявка",
                $"{clientName}\n{phone}",
                "callback"
            );
        }

        /// <summary>
        /// Показать уведомление о новом заказе
        /// </summary>
        public void ShowNewOrderNotification(int orderId, string clientName)
        {
            ShowToast(
                "📋 Новый заказ",
                $"#{orderId} - {clientName}",
                "order"
            );
        }

        /// <summary>
        /// Показать уведомление об изменении статуса
        /// </summary>
        public void ShowStatusChangeNotification(int orderId, string newStatus)
        {
            var emoji = newStatus switch
            {
                "В работе" => "🔧",
                "Завершен" => "✅",
                "Отменен" => "❌",
                _ => "📋"
            };

            ShowToast(
                $"{emoji} Заказ #{orderId}",
                $"Статус: {newStatus}",
                "status"
            );
        }

        /// <summary>
        /// Показать произвольное уведомление
        /// </summary>
        public void ShowToast(string title, string message, string tag = "general")
        {
            try
            {
                new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message)
                    .SetToastScenario(ToastScenario.Default)
                    .Show(toast =>
                    {
                        toast.Tag = tag;
                        toast.Group = "HQStudio";
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Toast error: {ex.Message}");
                // Fallback - показать в UI
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    // Можно показать внутреннее уведомление
                });
            }
        }

        /// <summary>
        /// Очистить все уведомления
        /// </summary>
        public void ClearAllNotifications()
        {
            try
            {
                ToastNotificationManagerCompat.History.Clear();
            }
            catch { }
        }
    }
}
