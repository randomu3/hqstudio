using HQStudio.Models;
using HQStudio.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HQStudio.Views
{
    public partial class ServicesView : UserControl
    {
        public ServicesView()
        {
            InitializeComponent();
        }

        private void Service_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement element && element.DataContext is Service service)
            {
                // Устанавливаем выбранную услугу и вызываем команду редактирования через ViewModel
                if (DataContext is ServicesViewModel vm)
                {
                    vm.SelectedService = service;
                    if (vm.EditServiceCommand.CanExecute(null))
                    {
                        vm.EditServiceCommand.Execute(null);
                    }
                }
            }
        }
    }
}
