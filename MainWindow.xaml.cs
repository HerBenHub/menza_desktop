using menza_admin.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace menza_admin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StatusLabel.Content = "Kapcsolat ellenőrzése...";
            try
            {
                // Server responds at root -> request "/"
                var text = await App.Api.GetAsync("/");
                StatusLabel.Content = $"Kapcsolat él! Válasz: {text}";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = "Kapcsolat sikertelen!";
                MessageBox.Show($"Hiba az adatkapcsolatban:\n{ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class User
        {
            public string Name { get; set; }
            public int Id { get; set; }
        }
    }
}


