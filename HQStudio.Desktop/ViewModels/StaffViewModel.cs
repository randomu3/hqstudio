using HQStudio.Services;
using HQStudio.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    public class StaffViewModel : BaseViewModel
    {
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;
        private readonly ApiCacheService _cache = ApiCacheService.Instance;
        private const string CacheKey = "staff";
        
        private StaffItem? _selectedUser;
        private bool _isLoading;
        private bool _isApiConnected;

        public ObservableCollection<StaffItem> Users { get; } = new();

        public StaffItem? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsApiConnected
        {
            get => _isApiConnected;
            set => SetProperty(ref _isApiConnected, value);
        }

        public bool ShowEmptyState => !IsLoading && Users.Count == 0 && IsApiConnected;

        public ICommand RefreshCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ResetPasswordCommand { get; }

        public StaffViewModel()
        {
            RefreshCommand = new RelayCommand(async _ => await LoadUsersAsync(forceRefresh: true));
            AddUserCommand = new RelayCommand(async _ => await AddUserAsync());
            EditUserCommand = new RelayCommand(async _ => await EditUserAsync(), _ => SelectedUser != null);
            ToggleActiveCommand = new RelayCommand(async _ => await ToggleActiveAsync(), _ => SelectedUser != null);
            DeleteUserCommand = new RelayCommand(async _ => await DeleteUserAsync(), _ => SelectedUser != null);
            ResetPasswordCommand = new RelayCommand(async _ => await ResetPasswordAsync(), _ => SelectedUser != null);
            
            _ = LoadUsersAsync();
        }

        private async Task LoadUsersAsync(bool forceRefresh = false)
        {
            if (!_settings.UseApi)
            {
                IsApiConnected = false;
                return;
            }

            IsLoading = true;

            try
            {
                if (!_apiService.IsConnected)
                {
                    await _apiService.CheckConnectionAsync();
                }

                if (!_apiService.IsConnected)
                {
                    IsApiConnected = false;
                    IsLoading = false;
                    return;
                }

                IsApiConnected = true;
                
                var users = await _cache.GetOrFetchAsync(
                    CacheKey,
                    async () => await _apiService.GetUsersAsync(),
                    TimeSpan.FromSeconds(30),
                    forceRefresh);
                
                if (users != null)
                {
                    Users.Clear();
                    foreach (var user in users)
                    {
                        Users.Add(new StaffItem
                        {
                            Id = user.Id,
                            Login = user.Login,
                            Name = user.Name,
                            Role = user.Role,
                            IsActive = user.IsActive,
                            IsOnline = user.IsOnline,
                            CreatedAt = user.CreatedAt
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadUsersAsync error: {ex.Message}");
                IsApiConnected = false;
            }

            IsLoading = false;
        }

        private async Task AddUserAsync()
        {
            var dialog = new EditUserDialog();
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                var request = new CreateApiUserRequest
                {
                    Login = dialog.UserLogin,
                    Name = dialog.UserName,
                    Password = dialog.UserPassword,
                    Role = dialog.UserRole
                };

                var result = await _apiService.CreateUserAsync(request);
                if (result != null)
                {
                    _cache.Invalidate(CacheKey);
                    await LoadUsersAsync(forceRefresh: true);
                    ConfirmDialog.ShowInfo("Успех", $"Сотрудник {result.Name} добавлен", ConfirmDialog.DialogType.Success);
                }
                else
                {
                    ConfirmDialog.ShowInfo("Ошибка", "Не удалось создать пользователя. Возможно, логин уже занят.", ConfirmDialog.DialogType.Warning);
                }
            }
        }

        private async Task EditUserAsync()
        {
            if (SelectedUser == null)
            {
                ConfirmDialog.ShowInfo("Внимание", "Сначала выберите сотрудника из списка", ConfirmDialog.DialogType.Warning);
                return;
            }

            var dialog = new EditUserDialog(SelectedUser);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                var request = new UpdateApiUserRequest
                {
                    Name = dialog.UserName,
                    Role = dialog.UserRole,
                    Password = string.IsNullOrEmpty(dialog.UserPassword) ? null : dialog.UserPassword
                };

                var success = await _apiService.UpdateUserAsync(SelectedUser.Id, request);
                if (success)
                {
                    _cache.Invalidate(CacheKey);
                    await LoadUsersAsync(forceRefresh: true);
                }
                else
                {
                    ConfirmDialog.ShowInfo("Ошибка", "Не удалось обновить пользователя", ConfirmDialog.DialogType.Warning);
                }
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (SelectedUser == null)
            {
                ConfirmDialog.ShowInfo("Внимание", "Сначала выберите сотрудника из списка", ConfirmDialog.DialogType.Warning);
                return;
            }

            if (SelectedUser.Login == "admin")
            {
                ConfirmDialog.ShowInfo("Ошибка", "Нельзя деактивировать администратора", ConfirmDialog.DialogType.Warning);
                return;
            }

            var action = SelectedUser.IsActive ? "деактивировать" : "активировать";
            var result = ConfirmDialog.Show(
                "Подтверждение",
                $"{action.Substring(0, 1).ToUpper() + action.Substring(1)} сотрудника \"{SelectedUser.Name}\"?",
                ConfirmDialog.DialogType.Question,
                "Да", "Нет");

            if (result)
            {
                var success = await _apiService.ToggleUserActiveAsync(SelectedUser.Id);
                if (success)
                {
                    _cache.Invalidate(CacheKey);
                    await LoadUsersAsync(forceRefresh: true);
                }
            }
        }

        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null)
            {
                ConfirmDialog.ShowInfo("Внимание", "Сначала выберите сотрудника из списка", ConfirmDialog.DialogType.Warning);
                return;
            }

            if (SelectedUser.Login == "admin")
            {
                ConfirmDialog.ShowInfo("Ошибка", "Нельзя удалить администратора", ConfirmDialog.DialogType.Warning);
                return;
            }

            var result = ConfirmDialog.Show(
                "Подтверждение удаления",
                $"Удалить сотрудника \"{SelectedUser.Name}\"?\n\nУчётная запись будет деактивирована, но сохранена в базе.",
                ConfirmDialog.DialogType.Warning,
                "Удалить", "Отмена");

            if (result)
            {
                var success = await _apiService.DeleteUserAsync(SelectedUser.Id);
                if (success)
                {
                    _cache.Invalidate(CacheKey);
                    await LoadUsersAsync(forceRefresh: true);
                }
            }
        }

        private async Task ResetPasswordAsync()
        {
            if (SelectedUser == null)
            {
                ConfirmDialog.ShowInfo("Внимание", "Сначала выберите сотрудника из списка", ConfirmDialog.DialogType.Warning);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[ResetPassword] SelectedUser: ID={SelectedUser.Id}, Login={SelectedUser.Login}, Name={SelectedUser.Name}");

            // Проверяем, сбрасывает ли пользователь свой собственный пароль
            var currentUser = _apiService.CurrentUser;
            System.Diagnostics.Debug.WriteLine($"[ResetPassword] CurrentUser: {(currentUser != null ? $"ID={currentUser.Id}, Login={currentUser.Login}" : "null")}");
            
            var isResettingOwnPassword = currentUser != null && currentUser.Id == SelectedUser.Id;

            var message = isResettingOwnPassword
                ? $"Вы собираетесь сбросить СВОЙ пароль!\n\nПосле сброса вы будете автоматически выведены из системы.\nНовый пароль будет равен логину ({SelectedUser.Login})."
                : $"Сбросить пароль для \"{SelectedUser.Name}\"?\n\nНовый пароль будет равен логину ({SelectedUser.Login}).\nПри следующем входе пользователь должен будет задать новый пароль.";

            var result = ConfirmDialog.Show(
                "Сброс пароля",
                message,
                isResettingOwnPassword ? ConfirmDialog.DialogType.Warning : ConfirmDialog.DialogType.Question,
                "Сбросить", "Отмена");

            if (result)
            {
                System.Diagnostics.Debug.WriteLine($"[ResetPassword] Calling API with ID={SelectedUser.Id}");
                var (success, error) = await _apiService.ResetUserPasswordAsync(SelectedUser.Id);
                System.Diagnostics.Debug.WriteLine($"[ResetPassword] Result: success={success}, error={error}");
                
                if (success)
                {
                    _cache.Invalidate(CacheKey);
                    
                    if (isResettingOwnPassword)
                    {
                        // Выходим из системы и показываем окно логина
                        ConfirmDialog.ShowInfo("Пароль сброшен", 
                            $"Ваш пароль сброшен.\nНовый пароль: {SelectedUser.Login}\n\nВы будете перенаправлены на экран входа.", 
                            ConfirmDialog.DialogType.Success);
                        
                        // Очищаем токен и выходим
                        _apiService.ClearToken();
                        
                        // Открываем окно логина
                        var loginWindow = new Views.LoginWindow();
                        loginWindow.Show();
                        
                        // Закрываем главное окно
                        Application.Current.MainWindow?.Close();
                    }
                    else
                    {
                        ConfirmDialog.ShowInfo("Успех", $"Пароль сброшен.\nНовый пароль: {SelectedUser.Login}", ConfirmDialog.DialogType.Success);
                    }
                }
                else
                {
                    var errorMsg = string.IsNullOrEmpty(error) ? "Не удалось сбросить пароль" : error;
                    ConfirmDialog.ShowInfo("Ошибка", errorMsg, ConfirmDialog.DialogType.Warning);
                }
            }
        }
    }

    public class StaffItem
    {
        public int Id { get; set; }
        public string Login { get; set; } = "";
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsOnline { get; set; }
        public DateTime CreatedAt { get; set; }

        public string RoleDisplay => Role switch
        {
            "Admin" => "Администратор",
            "Editor" => "Редактор",
            "Manager" => "Менеджер",
            _ => "Работник"
        };

        public string StatusText => IsOnline ? "В сети" : (IsActive ? "Не в сети" : "Неактивен");
        public string StatusColor => IsOnline ? "#4CAF50" : (IsActive ? "#707070" : "#F44336");
    }
}
