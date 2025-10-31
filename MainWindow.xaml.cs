using System.Windows;

namespace menza_admin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new Orders()); // Start with OrdersPage
        }

        private void NavigateOrders(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Orders());
        }

        private void NavigateMenus(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ManageMenus());
        }

        private void NavigateFoods(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ManageFoods());
        }
    }
}


