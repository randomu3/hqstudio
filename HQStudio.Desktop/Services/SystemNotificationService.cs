using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

// Используем алиасы для избежания конфликтов имён
using WinFormsNotifyIcon = System.Windows.Forms.NotifyIcon;
using WinFormsToolTipIcon = System.Windows.Forms.ToolTipIcon;
using DrawingIcon = System.Drawing.Icon;
using DrawingSystemIcons = System.Drawing.SystemIcons;

namespace HQStudio.Services
{
    /// <summary>
    /// Сервис для отображения системных уведомлений Windows (в трее)
    /// </summary>
    public class SystemNotificationService : INotifyPropertyChanged, IDisposable
    {
        private static SystemNotificationService? _instance;
        public static SystemNotificationService Instance => _instance ??= new SystemNotificationService();

        private WinFormsNotifyIcon? _notifyIcon;
        private Window? _mainWindow;
        private bool _isDisposed;

        /// <summary>
        /// Событие при клике на уведомление
        /// </summary>
        public event Action<string>? OnNotificationClicked;

        /// <summary>
        /// Событие PropertyChanged для INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        private SystemNotificationService() { }

        /// <summary>
        /// Инициализация сервиса с привязкой к главному окну
        /// </summary>
        public void Initialize(Window mainWindow)
        {
            _mainWindow = mainWindow;
            
            try
            {
                // Создаём иконку в трее
                _notifyIcon = new WinFormsNotifyIcon
                {
                    Visible = false,
                    Text = "HQ Studio"
                };

                // Загружаем иконку приложения
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
                if (File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new DrawingIcon(iconPath);
                }
                else
                {
                    // Используем системную иконку по умолчанию
                    _notifyIcon.Icon = DrawingSystemIcons.Application;
                }

                // Обработка клика на иконку в трее
                _notifyIcon.DoubleClick += (s, e) => RestoreMainWindow();
                
                // Обработка клика на balloon notification
                _notifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;

                // Показываем иконку когда окно свёрнуто
                _mainWindow.StateChanged += MainWindow_StateChanged;

                System.Diagnostics.Debug.WriteLine("SystemNotificationService initialized");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SystemNotificationService initialization error: {ex.Message}");
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (_mainWindow == null || _notifyIcon == null) return;

            // Показываем иконку в трее когда окно свёрнуто
            _notifyIcon.Visible = _mainWindow.WindowState == WindowState.Minimized;
        }

        private void NotifyIcon_BalloonTipClicked(object? sender, EventArgs e)
        {
            RestoreMainWindow();
            OnNotificationClicked?.Invoke("Callbacks");
        }

        /// <summary>
        /// Проверяет, свёрнуто ли главное окно
        /// </summary>
        public bool IsAppMinimized => _mainWindow?.WindowState == WindowState.Minimized;

        /// <summary>
        /// Показать системное уведомление Windows
        /// </summary>
        /// <param name="title">Заголовок уведомления</param>
        /// <param name="message">Текст уведомления</param>
        /// <param name="onClick">Действие при клике (опционально)</param>
        public void ShowNotification(string title, string message, Action? onClick = null)
        {
            if (_notifyIcon == null) return;

            try
            {
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    // Убеждаемся что иконка видна для показа уведомления
                    _notifyIcon.Visible = true;

                    // Показываем balloon notification
                    _notifyIcon.ShowBalloonTip(
                        timeout: 5000,
                        tipTitle: title,
                        tipText: message,
                        tipIcon: WinFormsToolTipIcon.Info
                    );

                    System.Diagnostics.Debug.WriteLine($"System notification shown: {title} - {message}");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ShowNotification error: {ex.Message}");
            }
        }

        /// <summary>
        /// Показать уведомление о новой заявке (только если приложение свёрнуто)
        /// </summary>
        /// <param name="name">Имя клиента</param>
        /// <param name="phone">Телефон клиента</param>
        public void ShowNewCallbackNotification(string name, string phone)
        {
            if (!IsAppMinimized) return;

            ShowNotification(
                "📞 Новая заявка",
                $"{name}\n{phone}"
            );
        }

        /// <summary>
        /// Показать уведомление о новом заказе (только если приложение свёрнуто)
        /// </summary>
        /// <param name="clientName">Имя клиента</param>
        /// <param name="orderId">ID заказа</param>
        public void ShowNewOrderNotification(string clientName, int orderId)
        {
            if (!IsAppMinimized) return;

            ShowNotification(
                "📋 Новый заказ",
                $"Заказ #{orderId}\nКлиент: {clientName}"
            );
        }

        /// <summary>
        /// Восстановить главное окно из свёрнутого состояния
        /// </summary>
        public void RestoreMainWindow()
        {
            if (_mainWindow == null) return;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
                _mainWindow.Focus();

                // Скрываем иконку в трее
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                }
            });
        }

        /// <summary>
        /// Скрыть иконку в трее
        /// </summary>
        public void HideTrayIcon()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                if (_mainWindow != null)
                {
                    _mainWindow.StateChanged -= MainWindow_StateChanged;
                }

                if (_notifyIcon != null)
                {
                    _notifyIcon.BalloonTipClicked -= NotifyIcon_BalloonTipClicked;
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
            }

            _isDisposed = true;
        }

        ~SystemNotificationService()
        {
            Dispose(false);
        }
    }
}
