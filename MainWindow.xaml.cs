using menza_admin.Models;
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

                // Example: Create a food item
                var foodRequest = new CreateFoodRequest
                {
                    //Ide kell bekötni a frontend inputokat
                    Name = "Teszter kaja",
                    Description = "TESZT - Klasszikus rántott sajt hasábburgonyával",
                    Price = 1990,
                    PictureId = "rantott_sajt_1",
                    Allergens = new System.Collections.Generic.List<long>
                    {
                        21127545734824960
                    }
                };

                var createdFood = await App.Api.CreateFoodAsync(foodRequest);
                MessageBox.Show($"Étel létrehozva: {createdFood.Name} (ID: {createdFood.Id})");
            }
            catch (Exception ex)
            {
                StatusLabel.Content = "Kapcsolat sikertelen!";
                MessageBox.Show($"Hiba az adatkapcsolatban:\n{ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}


