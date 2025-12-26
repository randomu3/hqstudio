using System.Windows.Controls;
using System.Windows.Input;
using HQStudio.ViewModels;

namespace HQStudio.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void StatsCard_Clients_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.NavigateToClientsCommand.Execute(null);
            }
        }

        private void StatsCard_Orders_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.NavigateToOrdersCommand.Execute(null);
            }
        }

        private void StatsCard_ActiveOrders_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.NavigateToActiveOrdersCommand.Execute(null);
            }
        }

        private void StatsCard_Revenue_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.NavigateToRevenueCommand.Execute(null);
            }
        }
    }
}
