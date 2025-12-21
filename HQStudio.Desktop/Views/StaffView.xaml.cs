using HQStudio.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Views
{
    public partial class StaffView : UserControl
    {
        public StaffView()
        {
            InitializeComponent();
        }

        private void User_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is StaffItem user)
            {
                // Одиночный клик - выбор
                if (DataContext is StaffViewModel vm)
                {
                    vm.SelectedUser = user;
                }

                // Двойной клик - редактирование
                if (e.ClickCount == 2)
                {
                    if (DataContext is StaffViewModel viewModel)
                    {
                        viewModel.EditUserCommand.Execute(null);
                    }
                }
            }
        }
    }
}
