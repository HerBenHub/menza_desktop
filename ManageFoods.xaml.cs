using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using menza_admin.Models;
using menza_admin.Services;

namespace menza_admin
{
    public partial class ManageFoods : Page
    {
        private ObservableCollection<Food> foodsList;
        private Dictionary<string, long> allergenMap; // Map allergen names to IDs

        public ManageFoods()
        {
            InitializeComponent();
            InitializeAllergenMap();
            InitializeData();
            SetupEventHandlers();
            LoadFoodsAsync();
        }

        private void InitializeAllergenMap()
        {
            // Map UI allergen names to their database IDs
            // TODO: These IDs should be fetched from the API, but for now using hardcoded values
            allergenMap = new Dictionary<string, long>
            {
                { "gluten", 1 },
                { "dairy", 2 },
                { "nuts", 3 },
                { "peanuts", 4 },
                { "sesame", 5 },
                { "soy", 6 },
                { "fish", 7 },
                { "shellfish", 8 }
            };
        }

        private void InitializeData()
        {
            foodsList = new ObservableCollection<Food>();
            FoodsDataGrid.ItemsSource = foodsList;
        }

        private void SetupEventHandlers()
        {
            AddFoodButton.Click += AddFoodButton_Click;
            ClearButton.Click += ClearButton_Click;
        }

        private async void AddFoodButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(FoodNameTextBox.Text))
            {
                MessageBox.Show("Kérem adja meg az étel nevét!", "Validáció", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PriceTextBox.Text, out int price) || price < 0)
            {
                MessageBox.Show("Kérem adjon meg érvényes árat!", "Validáció", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                AddFoodButton.IsEnabled = false;
                
                // Collect selected allergen IDs from checkboxes
                var allergenIds = new List<string>();
                if (GlutenCheckBox.IsChecked == true && allergenMap.ContainsKey("gluten")) 
                    allergenIds.Add(allergenMap["gluten"].ToString());
                if (DairyCheckBox.IsChecked == true && allergenMap.ContainsKey("dairy")) 
                    allergenIds.Add(allergenMap["dairy"].ToString());
                if (NutsCheckBox.IsChecked == true && allergenMap.ContainsKey("nuts")) 
                    allergenIds.Add(allergenMap["nuts"].ToString());
                if (PeanutsCheckBox.IsChecked == true && allergenMap.ContainsKey("peanuts")) 
                    allergenIds.Add(allergenMap["peanuts"].ToString());
                if (SesameCheckBox.IsChecked == true && allergenMap.ContainsKey("sesame")) 
                    allergenIds.Add(allergenMap["sesame"].ToString());
                if (SoyCheckBox.IsChecked == true && allergenMap.ContainsKey("soy")) 
                    allergenIds.Add(allergenMap["soy"].ToString());
                if (FishCheckBox.IsChecked == true && allergenMap.ContainsKey("fish")) 
                    allergenIds.Add(allergenMap["fish"].ToString());
                if (ShellfishCheckBox.IsChecked == true && allergenMap.ContainsKey("shellfish")) 
                    allergenIds.Add(allergenMap["shellfish"].ToString());

                // Create the data object
                var foodData = new
                {
                    name = FoodNameTextBox.Text,
                    description = DescriptionTextBox.Text,
                    price = price,
                    allergens = allergenIds
                };

                // Debug: Show what we're about to send
                var debugInfo = $"Sending to API:\nName: {foodData.name}\nPrice: {foodData.price}\nAllergen IDs: {string.Join(", ", allergenIds)}";
                System.Diagnostics.Debug.WriteLine(debugInfo);

                // Send as multipart/form-data with file
                using (var multipartContent = new MultipartFormDataContent())
                {
                    // Add JSON data
                    string jsonData = JsonSerializer.Serialize(foodData);
                    multipartContent.Add(new StringContent(jsonData), "data");

                    // Add placeholder image file (1x1 transparent PNG)
                    byte[] placeholderImage = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
                    var imageContent = new ByteArrayContent(placeholderImage);
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                    multipartContent.Add(imageContent, "file", "placeholder.png");

                    using (var httpClient = new HttpClient())
                    {
                        httpClient.BaseAddress = new Uri("http://localhost:3001");
                        httpClient.DefaultRequestHeaders.Add("X-Client-Type", "desktop");
                        
                        var response = await httpClient.PostAsync("/v1/food", multipartContent);
                        string responseText = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"Failed to create food. Status: {response.StatusCode}\nResponse: {responseText}");
                        }

                        // Deserialize the created food
                        var createdFood = JsonSerializer.Deserialize<Food>(responseText, Api.JsonOptions);
                        if (createdFood == null)
                        {
                            throw new Exception("Failed to deserialize response");
                        }
                        
                        foodsList.Add(createdFood);
                    }
                }

                MessageBox.Show("Étel sikeresen hozzáadva!", "Siker", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearAllFields();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Hiba az étel hozzáadása során:\n\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
                }
                MessageBox.Show(errorMessage, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                AddFoodButton.IsEnabled = true;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearAllFields();
        }

        private void ClearAllFields()
        {
            FoodNameTextBox.Clear();
            DescriptionTextBox.Clear();
            PriceTextBox.Clear();
            
            // Clear all allergen checkboxes
            GlutenCheckBox.IsChecked = false;
            DairyCheckBox.IsChecked = false;
            NutsCheckBox.IsChecked = false;
            PeanutsCheckBox.IsChecked = false;
            SesameCheckBox.IsChecked = false;
            SoyCheckBox.IsChecked = false;
            FishCheckBox.IsChecked = false;
            ShellfishCheckBox.IsChecked = false;
        }

        private async void LoadFoodsAsync()
        {
            try
            {
                var foods = await App.Api.GetAllFoodsAsync();
                foodsList.Clear();
                foreach (var food in foods)
                {
                    foodsList.Add(food);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az ételek betöltése során:\n\n{ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the button that was clicked
                var button = sender as Button;
                if (button == null) return;

                // Get the Food object from the button's DataContext
                var food = button.DataContext as Food;
                if (food == null) return;

                // Confirm deletion
                var result = MessageBox.Show(
                    $"Biztosan törölni szeretné a(z) '{food.Name}' ételt?\n\nEz a művelet nem vonható vissza!",
                    "Törlés megerősítése",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                // Disable the button during deletion
                button.IsEnabled = false;

                try
                {
                    // Call API to delete the food
                    await App.Api.DeleteFoodAsync(food.Id);

                    // Remove from local list
                    foodsList.Remove(food);

                    MessageBox.Show($"Az étel '{food.Name}' sikeresen törölve!", "Siker", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                finally
                {
                    button.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Hiba az étel törlése során:\n\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner Exception: {ex.InnerException.Message}";
                }
                MessageBox.Show(errorMessage, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the button that was clicked
                var button = sender as Button;
                if (button == null) return;

                // Get the Food object from the button's DataContext
                var food = button.DataContext as Food;
                if (food == null) return;

                // Check if the panel is already showing this food's description
                if (DescriptionPanel.Visibility == Visibility.Visible && 
                    DescriptionContentTextBlock.Text == food.Description)
                {
                    // Toggle off - hide the panel
                    DescriptionPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Show the description in the panel
                    DescriptionTitleTextBlock.Text = $"Leírás - {food.Name}";
                    DescriptionContentTextBlock.Text = food.Description;
                    DescriptionPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseDescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            DescriptionPanel.Visibility = Visibility.Collapsed;
        }
    }
}
