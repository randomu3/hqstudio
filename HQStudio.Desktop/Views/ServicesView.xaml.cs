using HQStudio.Models;
using HQStudio.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HQStudio.Views
{
    public partial class ServicesView : UserControl
    {
        private Border? _lastSelectedIndicator;
        
        public ServicesView()
        {
            InitializeComponent();
        }

        private void Service_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border cardBorder && cardBorder.DataContext is Service service)
            {
                if (DataContext is ServicesViewModel vm)
                {
                    // Сбрасываем предыдущее выделение
                    if (_lastSelectedIndicator != null)
                    {
                        _lastSelectedIndicator.Background = Brushes.Transparent;
                    }
                    
                    // Находим индикатор в текущей карточке
                    var grid = cardBorder.Child as Grid;
                    if (grid != null)
                    {
                        var indicator = grid.Children.OfType<Border>().FirstOrDefault(b => b.Name == "SelectionIndicator");
                        if (indicator != null)
                        {
                            indicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                            _lastSelectedIndicator = indicator;
                        }
                    }
                    
                    // Устанавливаем выбранную услугу
                    vm.SelectedService = service;
                    
                    // Двойной клик - редактирование
                    if (e.ClickCount == 2)
                    {
                        if (vm.EditServiceCommand.CanExecute(null))
                        {
                            vm.EditServiceCommand.Execute(null);
                        }
                    }
                }
            }
        }
    }
}
