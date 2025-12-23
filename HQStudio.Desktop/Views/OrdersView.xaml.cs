using HQStudio.Models;
using HQStudio.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HQStudio.Views
{
    public partial class OrdersView : UserControl
    {
        private Border? _selectedBorder;
        
        public OrdersView()
        {
            InitializeComponent();
        }

        private void Order_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Order order)
            {
                if (DataContext is OrdersViewModel vm)
                {
                    // Сбрасываем предыдущее выделение
                    if (_selectedBorder != null)
                    {
                        _selectedBorder.BorderBrush = Brushes.Transparent;
                    }
                    
                    // Выделяем текущий заказ
                    vm.SelectedOrder = order;
                    border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    _selectedBorder = border;
                    
                    // Двойной клик - редактирование
                    if (e.ClickCount == 2)
                    {
                        vm.EditOrder(order);
                    }
                }
            }
        }
    }
}
