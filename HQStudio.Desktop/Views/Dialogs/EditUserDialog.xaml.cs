using HQStudio.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace HQStudio.Views.Dialogs
{
    public partial class EditUserDialog : Window
    {
        public bool IsNew { get; }
        
        // Свойства для получения данных из формы
        public string UserLogin => UsernameBox.Text.Trim().ToLower();
        public string UserName => DisplayNameBox.Text.Trim();
        public string UserPassword => PasswordBox.Password;
        public string UserRole => RoleCombo.SelectedIndex == 0 ? "Admin" : "Manager";

        public EditUserDialog(StaffItem? user = null)
        {
            InitializeComponent();
            IsNew = user == null;
            
            TitleText.Text = IsNew ? "Новый сотрудник" : "Редактирование сотрудника";
            
            if (user != null)
            {
                LoadData(user);
                UsernameBox.IsEnabled = false; // Логин нельзя менять
            }
            
            Loaded += (s, e) => DisplayNameBox.Focus();
        }

        private void LoadData(StaffItem user)
        {
            DisplayNameBox.Text = user.Name;
            UsernameBox.Text = user.Login;
            RoleCombo.SelectedIndex = user.Role == "Admin" ? 0 : 1;
            IsActiveCheck.IsChecked = user.IsActive;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DisplayNameBox.Text))
            {
                MessageBox.Show("Введите имя сотрудника", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                DisplayNameBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(UsernameBox.Text))
            {
                MessageBox.Show("Введите логин", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameBox.Focus();
                return;
            }

            if (IsNew && string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Введите пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Cancel_Click(sender, e);

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1) DragMove();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Cancel_Click(sender, e);
            }
        }
    }
}
