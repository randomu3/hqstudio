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
        private StaffItem? _selectedUser;
        private bool _isLoading;

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

        public ICommand RefreshCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand DeleteUserCommand { get; }

        public StaffViewModel()
        {
            RefreshCommand = new RelayCommand(async _ => await LoadUsersAsync());
            AddUserCommand = new RelayCommand(async _ => await AddUserAsync());
            EditUserCommand = new RelayCommand(async _ => await EditUserAsync(), _ => SelectedUser != null);
            ToggleActiveCommand = new RelayCommand(async _ => await ToggleActiveAsync(), _ => SelectedUser != null);
            DeleteUserCommand = new RelayCommand(async _ => await DeleteUserAsync(), _ => SelectedUser != null);
            
            _ = LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            if (!_settings.UseApi)
            {
                MessageBox.Show("API отключён в настройках", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsLoading = true;

            try
            {
                var users = await _apiService.GetUsersAsync();
                
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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    await LoadUsersAsync();
                    MessageBox.Show($"Сотрудник {result.Name} добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Не удалось создать пользователя. Возможно, логин уже занят.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async Task EditUserAsync()
        {
            if (SelectedUser == null) return;

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
                    await LoadUsersAsync();
                }
                else
                {
                    MessageBox.Show("Не удалось обновить пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (SelectedUser == null) return;

            if (SelectedUser.Login == "admin")
            {
                MessageBox.Show("Нельзя деактивировать администратора", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var action = SelectedUser.IsActive ? "деактивировать" : "активировать";
            var result = MessageBox.Show(
                $"{action.Substring(0, 1).ToUpper() + action.Substring(1)} сотрудника \"{SelectedUser.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var success = await _apiService.ToggleUserActiveAsync(SelectedUser.Id);
                if (success)
                {
                    await LoadUsersAsync();
                }
            }
        }

        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null) return;

            if (SelectedUser.Login == "admin")
            {
                MessageBox.Show("Нельзя удалить администратора", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Удалить сотрудника \"{SelectedUser.Name}\"?\n\nУчётная запись будет деактивирована, но сохранена в базе.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var success = await _apiService.DeleteUserAsync(SelectedUser.Id);
                if (success)
                {
                    await LoadUsersAsync();
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
