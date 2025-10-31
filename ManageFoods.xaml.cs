using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using menza_admin.Models;
using menza_admin.Services;

namespace menza_admin
{
    /// <summary>
    /// Converts Food ID and PictureId to CDN image URL
    /// </summary>
    public class FoodImageUrlConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return null;

            if (values[0] == null || values[1] == null)
                return null;

            var foodId = values[0].ToString();
            var pictureId = values[1].ToString();

            if (string.IsNullOrEmpty(pictureId) || pictureId == "placeholder_img_123")
                return null;

            try
            {
                // Build the CDN URL: https://cdn-canteen.kenderesi.hu/food/{foodId}/{pictureId}.webp
                string imageUrl = $"https://cdn-canteen.kenderesi.hu/food/{foodId}/{pictureId}.webp";
                System.Diagnostics.Debug.WriteLine($"Loading image: {imageUrl}");

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageUrl, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.DecodePixelWidth = 100;
                
                // This is key - handle download completion
                bitmap.DownloadCompleted += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Successfully loaded: {imageUrl}");
                };
                bitmap.DownloadFailed += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load: {imageUrl} - {e.ErrorException?.Message}");
                };
                
                bitmap.EndInit();
                
                // Don't freeze immediately - let it download first
                if (bitmap.IsDownloading)
                {
                    System.Diagnostics.Debug.WriteLine($"Image is downloading: {imageUrl}");
                }
                
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load image: {ex.Message}");
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class ManageFoods : Page
    {
        private ObservableCollection<Food> foodsList;
        private Dictionary<string, long> allergenMap; // Map allergen names to IDs
        private byte[] selectedImageData;
        private string selectedImageFileName;

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
            // Map UI allergen names to their database IDs (from the actual database)
            allergenMap = new Dictionary<string, long>
            {
                { "gluten", 21936604557870080 },      // Glutén
                { "dairy", 21936604562064384 },       // Tejtermék
                { "nuts", 21936604562064385 },        // Dióféle
                { "peanuts", 21936604562064386 },     // Tojás (keeping old key name for now)
                { "soy", 21936604562064387 },         // Szója
                { "fish", 21936604562064388 },        // Hal
                { "shellfish", 21936604562064389 },   // Rákféle
                { "sesame", 21936604562064390 }       // Szezám
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
                System.Diagnostics.Debug.WriteLine($"Allergen count being sent: {allergenIds.Count}");

                // Send as multipart/form-data with file
                using (var multipartContent = new MultipartFormDataContent())
                {
                    // Add JSON data
                    string jsonData = JsonSerializer.Serialize(foodData);
                    multipartContent.Add(new StringContent(jsonData), "data");

                    // Add image file - use uploaded image if available, otherwise use placeholder
                    byte[] imageData;
                    string fileName;
                    string contentType;

                    if (selectedImageData != null && selectedImageData.Length > 0)
                    {
                        // Use uploaded image
                        imageData = selectedImageData;
                        fileName = selectedImageFileName ?? "uploaded_image.jpg";
                        
                        // Determine content type from file extension
                        string extension = System.IO.Path.GetExtension(fileName).ToLower();
                        if (extension == ".jpg" || extension == ".jpeg")
                        {
                            contentType = "image/jpeg";
                        }
                        else if (extension == ".png")
                        {
                            contentType = "image/png";
                        }
                        else if (extension == ".bmp")
                        {
                            contentType = "image/bmp";
                        }
                        else
                        {
                            contentType = "image/jpeg";
                        }
                    }
                    else
                    {
                        // Use placeholder image
                        imageData = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
                        fileName = "placeholder.png";
                        contentType = "image/png";
                    }

                    var imageContent = new ByteArrayContent(imageData);
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                    multipartContent.Add(imageContent, "file", fileName);

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

                        // Debug: Show the API response
                        System.Diagnostics.Debug.WriteLine($"API Response: {responseText}");

                        // Deserialize the created food
                        var createdFood = JsonSerializer.Deserialize<Food>(responseText, Api.JsonOptions);
                        if (createdFood == null)
                        {
                            throw new Exception("Failed to deserialize response");
                        }
                        
                        // Debug: Check allergens in deserialized food
                        System.Diagnostics.Debug.WriteLine($"Created food has {createdFood.Allergens?.Count ?? 0} allergens");
                        if (createdFood.Allergens != null)
                        {
                            foreach (var allergen in createdFood.Allergens)
                            {
                                System.Diagnostics.Debug.WriteLine($"  - Allergen: {allergen.Name} (ID: {allergen.Id})");
                            }
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
            
            // Clear image
            selectedImageData = null;
            selectedImageFileName = null;
            ImagePreview.Source = null;
            ImagePreviewBorder.Visibility = Visibility.Collapsed;
            ImageFileNameText.Text = "Nincs kiválasztott kép";
        }

        private async void LoadFoodsAsync()
        {
            try
            {
                var foods = await App.Api.GetAllFoodsAsync();
                foodsList.Clear();
                
                System.Diagnostics.Debug.WriteLine($"Loaded {foods.Count} foods from API");
                
                foreach (var food in foods)
                {
                    System.Diagnostics.Debug.WriteLine($"Food: {food.Name}, Allergens: {food.Allergens?.Count ?? 0}");
                    foodsList.Add(food);
                }
                
                // Force UI update to trigger image loading
                await System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                {
                    FoodsDataGrid.Items.Refresh();
                }, System.Windows.Threading.DispatcherPriority.Render);
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

        private void UploadImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Válasszon egy képet",
                    Filter = "Képfájlok (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Minden fájl (*.*)|*.*",
                    FilterIndex = 1
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // Read the image file
                    selectedImageFileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                    selectedImageData = System.IO.File.ReadAllBytes(openFileDialog.FileName);

                    // Display preview
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ImagePreview.Source = bitmap;
                    ImagePreviewBorder.Visibility = Visibility.Visible;
                    ImageFileNameText.Text = selectedImageFileName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a kép betöltése során:\n\n{ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveImageButton_Click(object sender, RoutedEventArgs e)
        {
            selectedImageData = null;
            selectedImageFileName = null;
            ImagePreview.Source = null;
            ImagePreviewBorder.Visibility = Visibility.Collapsed;
            ImageFileNameText.Text = "Nincs kiválasztott kép";
        }
    }
}
