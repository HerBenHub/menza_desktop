using menza_admin.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

//Ez a frontend logikáját tarta

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

        private async void AddFoodButton_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}


