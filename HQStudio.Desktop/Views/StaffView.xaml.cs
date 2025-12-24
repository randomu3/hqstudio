using HQStudio.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HQStudio.Views
{
    public partial class StaffView : UserControl
    {
        private Border? _lastSelectedIndicator;
        
        public StaffView()
        {
            InitializeComponent();
        }

        private void User_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border cardBorder && cardBorder.DataContext is StaffItem user)
            {
                if (DataContext is StaffViewModel vm)
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
                    
                    // Устанавливаем выбранного пользователя
                    vm.SelectedUser = user;

                    // Двойной клик - редактирование
                    if (e.ClickCount == 2)
                    {
                        vm.EditUserCommand.Execute(null);
                    }
                }
            }
        }
    }
}
