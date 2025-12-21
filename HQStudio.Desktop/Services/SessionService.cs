using System.Net.Http.Json;
using System.Timers;

namespace HQStudio.Services
{
    /// <summary>
    /// Сервис управления сессией пользователя - отправляет heartbeat для отображения онлайн-статуса
    /// </summary>
    public class SessionService
    {
        private static SessionService? _instance;
        public static SessionService Instance => _instance ??= new SessionService();

        private readonly ApiService _apiService = ApiService.Instance;
        private readonly System.Timers.Timer _heartbeatTimer;
        private int? _sessionId;
        private string _deviceId;
        private string _deviceName;

        public bool IsSessionActive => _sessionId.HasValue;
        public int? CurrentSessionId => _sessionId;

        public event EventHandler<int>? PendingSyncChanged;

        private SessionService()
        {
            _deviceId = GetDeviceId();
            _deviceName = Environment.MachineName;
            
            _heartbeatTimer = new System.Timers.Timer(20000); // 20 секунд
            _heartbeatTimer.Elapsed += async (s, e) => await SendHeartbeatAsync();
            _heartbeatTimer.AutoReset = true;
        }

        /// <summary>
        /// Начать сессию при запуске приложения
        /// </summary>
        public async Task StartSessionAsync(int userId = 1)
        {
            try
            {
                var result = await _apiService.StartSessionAsync(_deviceId, _deviceName, userId);
                if (result != null)
                {
                    _sessionId = result.Id;
                    _heartbeatTimer.Start();
                    System.Diagnostics.Debug.WriteLine($"Session started: {_sessionId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start session: {ex.Message}");
            }
        }

        /// <summary>
        /// Завершить сессию при закрытии приложения
        /// </summary>
        public async Task EndSessionAsync()
        {
            _heartbeatTimer.Stop();
            
            if (_sessionId.HasValue)
            {
                try
                {
                    await _apiService.EndSessionAsync(_sessionId.Value);
                    System.Diagnostics.Debug.WriteLine($"Session ended: {_sessionId}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to end session: {ex.Message}");
                }
                _sessionId = null;
            }
        }

        private async Task SendHeartbeatAsync()
        {
            if (!_sessionId.HasValue) return;

            try
            {
                var result = await _apiService.SendHeartbeatAsync(_sessionId.Value);
                if (result != null)
                {
                    PendingSyncChanged?.Invoke(this, result.PendingSync);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Heartbeat failed: {ex.Message}");
            }
        }

        private string GetDeviceId()
        {
            // Используем комбинацию имени машины и имени пользователя как уникальный ID
            var machineId = Environment.MachineName + "_" + Environment.UserName;
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(machineId)).Replace("=", "");
        }
    }
}
