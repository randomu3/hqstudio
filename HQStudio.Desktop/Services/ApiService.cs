using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HQStudio.Models;

namespace HQStudio.Services
{
    public class ApiService
    {
        private static ApiService? _instance;
        public static ApiService Instance => _instance ??= new ApiService();

        private HttpClient _http;
        private string? _token;
        private string _baseUrl;
        
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public bool IsConnected { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
        public ApiUser? CurrentUser { get; private set; }
        public string BaseUrl => _baseUrl;

        private ApiService()
        {
            _baseUrl = SettingsService.Instance.ApiUrl;
            _http = CreateHttpClient(_baseUrl);
            
            // Подписываемся на изменение URL API
            SettingsService.Instance.ApiUrlChanged += OnApiUrlChanged;
        }

        private HttpClient CreateHttpClient(string baseUrl)
        {
            var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // Идентификатор Desktop клиента для снятия ограничений на бекенде
            client.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");
            client.Timeout = TimeSpan.FromSeconds(10);
            return client;
        }

        private void OnApiUrlChanged(string newUrl)
        {
            _baseUrl = newUrl;
            var oldToken = _token;
            _http.Dispose();
            _http = CreateHttpClient(newUrl);
            
            // Восстанавливаем токен если был
            if (!string.IsNullOrEmpty(oldToken))
            {
                SetToken(oldToken);
            }
            
            IsConnected = false;
        }

        /// <summary>
        /// Принудительно переинициализирует HttpClient с текущим URL из настроек
        /// </summary>
        public void Reinitialize()
        {
            OnApiUrlChanged(SettingsService.Instance.ApiUrl);
        }

        public void SetToken(string token)
        {
            _token = token;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public void ClearToken()
        {
            _token = null;
            _http.DefaultRequestHeaders.Authorization = null;
            CurrentUser = null;
        }

        // Auth
        public async Task<LoginResult?> LoginAsync(string login, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] LoginAsync: login={login}, url={_baseUrl}/api/auth/login");
                var response = await _http.PostAsJsonAsync("/api/auth/login", new { login, password });
                System.Diagnostics.Debug.WriteLine($"[ApiService] LoginAsync: StatusCode={response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResult>();
                    if (result != null)
                    {
                        SetToken(result.Token);
                        CurrentUser = result.User;
                        IsConnected = true;
                        System.Diagnostics.Debug.WriteLine($"[ApiService] LoginAsync: Success, user={result.User.Name}, mustChangePassword={result.MustChangePassword}");
                    }
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[ApiService] LoginAsync: Failed, error={errorContent}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiService] LoginAsync: Exception={ex.Message}");
                IsConnected = false;
            }
            return null;
        }

        // Change Password
        public async Task<(bool Success, string? Error)> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/auth/change-password", new { currentPassword, newPassword });
                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
                
                var error = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorObj = JsonSerializer.Deserialize<Dictionary<string, string>>(error, _jsonOptions);
                    if (errorObj != null && errorObj.TryGetValue("message", out var message))
                    {
                        return (false, message);
                    }
                }
                catch { }
                
                return (false, $"Ошибка: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // Clients
        public async Task<List<ApiClient>> GetClientsAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<ApiClient>>("/api/clients");
                return result ?? new();
            }
            catch { return new(); }
        }

        public async Task<(ApiClient? Client, string? Error)> CreateClientAsync(ApiClient client)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/clients", client);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<ApiClient>();
                    return (created, null);
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    return (null, "Клиент с таким номером телефона уже существует");
                }
                
                return (null, $"Ошибка сервера: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (null, $"Ошибка подключения: {ex.Message}");
            }
        }

        public async Task<bool> UpdateClientAsync(int id, ApiClient client)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/clients/{id}", client);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteClientAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/clients/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Services
        public async Task<List<ApiServiceItem>> GetServicesAsync(bool activeOnly = false)
        {
            try
            {
                var response = await _http.GetAsync($"/api/services?activeOnly={activeOnly}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<ApiServiceItem>>(json, _jsonOptions) ?? new();
                }
                return new();
            }
            catch { return new(); }
        }

        public async Task<ApiServiceItem?> CreateServiceAsync(ApiServiceItem service)
        {
            try
            {
                var json = JsonSerializer.Serialize(service, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync("/api/services", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ApiServiceItem>(responseJson, _jsonOptions);
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> UpdateServiceAsync(int id, ApiServiceItem service)
        {
            try
            {
                var json = JsonSerializer.Serialize(service, _jsonOptions);
                System.Diagnostics.Debug.WriteLine($"[Desktop API] UpdateService {id}: JSON={json}");
                System.Diagnostics.Debug.WriteLine($"[Desktop API] UpdateService {id}: Icon={service.Icon}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _http.PutAsync($"/api/services/{id}", content);
                
                System.Diagnostics.Debug.WriteLine($"[Desktop API] UpdateService {id}: Response={response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[Desktop API] UpdateService {id}: Error={errorBody}");
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Desktop API] UpdateService {id}: Exception={ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteServiceAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/services/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Orders
        public async Task<OrdersResponse?> GetOrdersAsync(int page = 1, int pageSize = 20, string? status = null)
        {
            try
            {
                var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
                if (!string.IsNullOrEmpty(status)) query.Add($"status={status}");
                var url = "/api/orders?" + string.Join("&", query);
                var response = await _http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[API] Orders JSON: {json.Substring(0, Math.Min(500, json.Length))}...");
                    var result = JsonSerializer.Deserialize<OrdersResponse>(json, _jsonOptions);
                    if (result?.Items?.Any() == true)
                    {
                        var first = result.Items.First();
                        System.Diagnostics.Debug.WriteLine($"[API] First order: Id={first.Id}, ClientId={first.ClientId}, Client={first.Client?.Name ?? "NULL"}");
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[API] GetOrdersAsync error: {ex.Message}");
            }
            return null;
        }

        public async Task<ApiOrder?> CreateOrderAsync(CreateOrderRequest order)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/orders", order);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<ApiOrder>();
            }
            catch { }
            return null;
        }

        public async Task<bool> UpdateOrderStatusAsync(int id, string status)
        {
            try
            {
                // Конвертируем строку статуса в число для API
                int statusValue = status switch
                {
                    "New" => 0,
                    "InProgress" => 1,
                    "Completed" => 2,
                    "Cancelled" => 3,
                    _ => 2 // По умолчанию Completed
                };
                
                var response = await _http.PutAsJsonAsync($"/api/orders/{id}/status", statusValue);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/orders/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Callbacks
        public async Task<List<ApiCallback>> GetCallbacksAsync(string? status = null, string? source = null)
        {
            try
            {
                var query = new List<string>();
                if (status != null) query.Add($"status={status}");
                if (source != null) query.Add($"source={source}");
                var url = "/api/callbacks" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
                
                var response = await _http.GetAsync(url);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Не авторизован - нужно залогиниться
                    return new();
                }
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = System.Text.Json.JsonSerializer.Deserialize<List<ApiCallback>>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return result ?? new();
                }
                
                return new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<ApiCallback?> GetCallbackAsync(int id)
        {
            try
            {
                return await _http.GetFromJsonAsync<ApiCallback>($"/api/callbacks/{id}");
            }
            catch { return null; }
        }

        public async Task<ApiCallback?> CreateCallbackAsync(CreateCallbackRequest callback)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/callbacks/manual", callback);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<ApiCallback>();
            }
            catch { }
            return null;
        }

        public async Task<ApiCallback?> UpdateCallbackAsync(int id, UpdateCallbackRequest callback)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/callbacks/{id}", callback);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<ApiCallback>();
            }
            catch { }
            return null;
        }

        public async Task<bool> UpdateCallbackStatusAsync(int id, string status)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/callbacks/{id}/status", status);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<CallbackStats?> GetCallbackStatsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<CallbackStats>("/api/callbacks/stats");
            }
            catch { return null; }
        }

        public async Task<bool> DeleteCallbackAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/callbacks/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Dashboard
        public async Task<DashboardStats?> GetDashboardStatsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<DashboardStats>("/api/dashboard", _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        // Check connection
        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                var response = await _http.GetAsync("/api/services?activeOnly=true");
                IsConnected = response.IsSuccessStatusCode;
                return IsConnected;
            }
            catch
            {
                IsConnected = false;
                return false;
            }
        }

        // Sessions
        public async Task<SessionResult?> StartSessionAsync(string deviceId, string deviceName, int? userId = null)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/sessions/start", new { deviceId, deviceName, userId });
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<SessionResult>();
            }
            catch { }
            return null;
        }

        public async Task<HeartbeatResult?> SendHeartbeatAsync(int sessionId)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/sessions/heartbeat", new { sessionId });
                if (response.IsSuccessStatusCode)
                {
                    IsConnected = true;
                    return await response.Content.ReadFromJsonAsync<HeartbeatResult>();
                }
            }
            catch { IsConnected = false; }
            return null;
        }

        public async Task EndSessionAsync(int sessionId)
        {
            try
            {
                await _http.PostAsJsonAsync("/api/sessions/end", new { sessionId });
            }
            catch { }
        }

        public async Task<List<ActiveUser>> GetActiveUsersAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<ActiveUser>>("/api/sessions/active");
                return result ?? new();
            }
            catch { return new(); }
        }

        // Activity Log
        public async Task<ActivityLogResponse?> GetActivityLogsAsync(int page = 1, int pageSize = 50, string? source = null, int? userId = null)
        {
            try
            {
                var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
                if (!string.IsNullOrEmpty(source)) query.Add($"source={source}");
                if (userId.HasValue) query.Add($"userId={userId}");
                var url = "/api/activitylog?" + string.Join("&", query);
                return await _http.GetFromJsonAsync<ActivityLogResponse>(url);
            }
            catch { return null; }
        }

        public async Task<ActivityLogStats?> GetActivityLogStatsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<ActivityLogStats>("/api/activitylog/stats");
            }
            catch { return null; }
        }

        public async Task<bool> LogActivityAsync(string action, string? entityType = null, int? entityId = null, string? details = null)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/activitylog", new
                {
                    action,
                    entityType,
                    entityId,
                    details,
                    source = "Desktop"
                });
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Callback with CallbackData (for offline sync)
        public async Task<ApiCallback?> CreateCallbackAsync(CallbackData callback)
        {
            try
            {
                var request = new CreateCallbackRequest
                {
                    Name = callback.Name,
                    Phone = callback.Phone,
                    CarModel = callback.CarModel,
                    LicensePlate = callback.LicensePlate,
                    Message = callback.Message,
                    Source = Enum.TryParse<RequestSource>(callback.Source, out var src) ? src : RequestSource.WalkIn,
                    SourceDetails = callback.SourceDetails
                };
                var response = await _http.PostAsJsonAsync("/api/callbacks/manual", request);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<ApiCallback>();
            }
            catch { }
            return null;
        }

        // Users
        public async Task<List<ApiUserDetail>> GetUsersAsync()
        {
            try
            {
                var result = await _http.GetFromJsonAsync<List<ApiUserDetail>>("/api/users");
                return result ?? new();
            }
            catch { return new(); }
        }

        public async Task<ApiUserDetail?> CreateUserAsync(CreateApiUserRequest user)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/api/users", user);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<ApiUserDetail>();
            }
            catch { }
            return null;
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateApiUserRequest user)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/users/{id}", user);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> ToggleUserActiveAsync(int id)
        {
            try
            {
                var response = await _http.PutAsync($"/api/users/{id}/toggle-active", null);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/users/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<(bool Success, string? Error)> ResetUserPasswordAsync(int id)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ResetUserPasswordAsync: Sending request for user ID={id}");
                var response = await _http.PostAsync($"/api/users/{id}/reset-password", null);
                System.Diagnostics.Debug.WriteLine($"ResetUserPasswordAsync: Response status {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
                
                var error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"ResetUserPasswordAsync: Error response: {error}");
                
                // Обработка специфичных ошибок
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, $"Пользователь с ID {id} не найден в базе данных");
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return (false, "Нет прав для сброса пароля");
                }
                
                // Пытаемся распарсить JSON ошибку
                try
                {
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(error);
                    if (errorObj != null && errorObj.TryGetValue("message", out var message))
                    {
                        return (false, message);
                    }
                }
                catch { }
                
                return (false, $"Ошибка сервера: {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"ResetUserPasswordAsync: HttpException: {ex.Message}");
                return (false, "Сервер недоступен. Проверьте подключение к API.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ResetUserPasswordAsync: Exception: {ex.Message}");
                return (false, ex.Message);
            }
        }
    }

    // Session DTOs
    public class SessionResult
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string DeviceId { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public class HeartbeatResult
    {
        public bool Success { get; set; }
        public DateTime ServerTime { get; set; }
        public int PendingSync { get; set; }
    }

    public class ActiveUser
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string UserRole { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime LastSeen { get; set; }
        public string DeviceName { get; set; } = "";
    }

    public class CallbackData
    {
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Message { get; set; }
        public string Source { get; set; } = "WalkIn";
        public string? SourceDetails { get; set; }
    }

    // API DTOs
    public class LoginResult
    {
        public string Token { get; set; } = "";
        public ApiUser User { get; set; } = new();
        public bool MustChangePassword { get; set; }
    }

    public class ApiUser
    {
        public int Id { get; set; }
        public string Login { get; set; } = "";
        public string Name { get; set; } = "";
        [System.Text.Json.Serialization.JsonConverter(typeof(RoleConverter))]
        public string Role { get; set; } = "";
    }

    // Конвертер для Role (может приходить как число или строка)
    public class RoleConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            if (reader.TokenType == System.Text.Json.JsonTokenType.Number)
            {
                var num = reader.GetInt32();
                return num switch
                {
                    0 => "Admin",
                    1 => "Editor", 
                    2 => "Manager",
                    _ => "User"
                };
            }
            return reader.GetString() ?? "User";
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    public class ApiClient
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Email { get; set; }
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ApiServiceItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Price { get; set; } = "";
        public string? Image { get; set; }
        public string Icon { get; set; } = "🔧";
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }

    public class ApiOrder
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public ApiClient? Client { get; set; }
        public int Status { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class OrdersResponse
    {
        public List<ApiOrder> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class CreateOrderRequest
    {
        public int ClientId { get; set; }
        public List<int> ServiceIds { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
    }

    public enum RequestSource
    {
        Website,
        Phone,
        WalkIn,
        Email,
        Messenger,
        Referral,
        Other
    }

    public class ApiCallback
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Message { get; set; }
        public int Status { get; set; } // API возвращает enum как число
        public int Source { get; set; } // API возвращает enum как число
        public string? SourceDetails { get; set; }
        public int? AssignedUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class CreateCallbackRequest
    {
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Message { get; set; }
        public RequestSource? Source { get; set; }
        public string? SourceDetails { get; set; }
    }

    public class UpdateCallbackRequest
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? CarModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Message { get; set; }
        public string? Status { get; set; }
        public RequestSource? Source { get; set; }
        public string? SourceDetails { get; set; }
    }

    public class CallbackStats
    {
        public int TotalNew { get; set; }
        public int TotalProcessing { get; set; }
        public int TotalCompleted { get; set; }
        public int TodayCount { get; set; }
        public int WeekCount { get; set; }
        public int MonthCount { get; set; }
        public List<SourceStat> BySource { get; set; } = new();
    }

    public class SourceStat
    {
        public RequestSource Source { get; set; }
        public int Count { get; set; }
    }

    public class DashboardStats
    {
        public int TotalClients { get; set; }
        public int TotalOrders { get; set; }
        public int NewCallbacks { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int OrdersInProgress { get; set; }
        public int CompletedThisMonth { get; set; }
        public int NewSubscribers { get; set; }
        public List<ServiceStat> PopularServices { get; set; } = new();
        public List<RecentOrderStat> RecentOrders { get; set; } = new();
    }

    public class ServiceStat
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    public class RecentOrderStat
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = "";
        public int Status { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Activity Log DTOs
    public class ActivityLogResponse
    {
        public List<ActivityLogEntry> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ActivityLogEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Action { get; set; } = "";
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? Details { get; set; }
        public string Source { get; set; } = "";
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }

        public string FormattedDate => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        public string SourceIcon => Source switch
        {
            "Desktop" => "🖥️",
            "Web" => "🌐",
            "API" => "⚙️",
            _ => "❓"
        };
    }

    public class ActivityLogStats
    {
        [System.Text.Json.Serialization.JsonPropertyName("totalToday")]
        public int TotalToday { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("totalWeek")]
        public int TotalWeek { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("totalAll")]
        public int TotalAll { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("bySource")]
        public List<ActivitySourceStat> BySource { get; set; } = new();
        
        [System.Text.Json.Serialization.JsonPropertyName("byUser")]
        public List<ActivityUserStat> ByUser { get; set; } = new();
    }

    public class ActivitySourceStat
    {
        [System.Text.Json.Serialization.JsonPropertyName("source")]
        public string Source { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class ActivityUserStat
    {
        [System.Text.Json.Serialization.JsonPropertyName("userId")]
        public int UserId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("userName")]
        public string UserName { get; set; } = "";
        
        [System.Text.Json.Serialization.JsonPropertyName("count")]
        public int Count { get; set; }
        
        public override string ToString() => UserName;
    }

    // User DTOs
    public class ApiUserDetail
    {
        public int Id { get; set; }
        public string Login { get; set; } = "";
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsOnline { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool CanAccessWeb { get; set; } = true;
        public bool CanAccessDesktop { get; set; } = true;
        public string? WebRole { get; set; }
        public string? DesktopRole { get; set; }
    }

    public class CreateApiUserRequest
    {
        public string Login { get; set; } = "";
        public string Name { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "Manager";
        public bool CanAccessWeb { get; set; } = true;
        public bool CanAccessDesktop { get; set; } = true;
        public string? WebRole { get; set; }
        public string? DesktopRole { get; set; }
    }

    public class UpdateApiUserRequest
    {
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public string? Password { get; set; }
        public bool? CanAccessWeb { get; set; }
        public bool? CanAccessDesktop { get; set; }
        public string? WebRole { get; set; }
        public string? DesktopRole { get; set; }
    }
}
