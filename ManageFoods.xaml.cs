using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using menza_admin.Models;
using menza_admin.Services;

namespace menza_admin
{
    public partial class ManageFoods : Page
    {
        private Api _api;
        private ObservableCollection<FoodViewModel> _foods;
        private byte[] _selectedImageData;
        private string _selectedImageFileName;

        public ManageFoods()
        {
            InitializeComponent();
            InitializeApi();
            Loaded += ManageFoods_Loaded;
        }

        private void InitializeApi()
        {
            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var apiUrl = configuration["ApiBaseUrl"];
                
                if (string.IsNullOrEmpty(apiUrl))
                {
                    MessageBox.Show("API URL is not configured in appsettings.json", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                _api = new Api(apiUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize API: {ex.Message}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ManageFoods_Loaded(object sender, RoutedEventArgs e)
        {
            if (_api == null)
            {
                LoadingText.Text = "API not initialized. Please check configuration.";
                LoadingText.Visibility = Visibility.Visible;
                return;
            }
            
            await LoadFoods();
        }

        private async Task LoadFoods()
        {
            try
            {
                LoadingText.Visibility = Visibility.Visible;
                FoodsDataGrid.Visibility = Visibility.Collapsed;

                var response = await _api.GetAsync("/v1/food");
                
                // Log the raw response for debugging
                System.Diagnostics.Debug.WriteLine("API Response:");
                System.Diagnostics.Debug.WriteLine(response);
                
                var foods = JsonSerializer.Deserialize<List<Food>>(response, Api.JsonOptions);

                if (foods == null || foods.Count == 0)
                {
                    _foods = new ObservableCollection<FoodViewModel>();
                    FoodsDataGrid.ItemsSource = _foods;
                    LoadingText.Text = "No foods found";
                    LoadingText.Visibility = Visibility.Visible;
                    return;
                }

                _foods = new ObservableCollection<FoodViewModel>(
                    foods.Select(f => new FoodViewModel
                    {
                        Id = f.Id,
                        Name = f.Name ?? "",
                        Description = f.Description ?? "",
                        Price = f.Price,
                        PictureId = f.PictureId ?? "",
                        Allergens = f.Allergens ?? new List<Allergen>(),
                        AllergensDisplay = (f.Allergens != null && f.Allergens.Any()) 
                            ? string.Join(", ", f.Allergens.Select(a => a.Name))
                            : "None"
                    })
                );

                FoodsDataGrid.ItemsSource = _foods;

                LoadingText.Visibility = Visibility.Collapsed;
                FoodsDataGrid.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LoadingText.Text = $"Error loading foods: {ex.Message}";
                LoadingText.Visibility = Visibility.Visible;
                MessageBox.Show($"Failed to load foods: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddFoodButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the form
            FoodNameTextBox.Text = string.Empty;
            FoodDescriptionTextBox.Text = string.Empty;
            FoodPriceTextBox.Text = string.Empty;
            FoodAllergensTextBox.Text = string.Empty;
            SelectedImageText.Text = "No image selected";
            ImagePreviewBorder.Visibility = Visibility.Collapsed;
            _selectedImageData = null;
            _selectedImageFileName = null;

            // Show dialog
            DialogOverlay.Visibility = Visibility.Visible;
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Select Food Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _selectedImageFileName = Path.GetFileName(openFileDialog.FileName);
                    _selectedImageData = File.ReadAllBytes(openFileDialog.FileName);
                    
                    SelectedImageText.Text = _selectedImageFileName;

                    // Show preview
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    
                    ImagePreview.Source = bitmap;
                    ImagePreviewBorder.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void SaveFood_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(FoodNameTextBox.Text))
                {
                    MessageBox.Show("Please enter a food name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(FoodDescriptionTextBox.Text))
                {
                    MessageBox.Show("Please enter a description.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(FoodPriceTextBox.Text, out int price) || price <= 0)
                {
                    MessageBox.Show("Please enter a valid price.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Parse allergens
                List<string> allergens = new List<string>();
                if (!string.IsNullOrWhiteSpace(FoodAllergensTextBox.Text))
                {
                    allergens = FoodAllergensTextBox.Text
                        .Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }

                // Create request
                var request = new CreateFoodRequest
                {
                    Name = FoodNameTextBox.Text,
                    Description = FoodDescriptionTextBox.Text,
                    Price = price,
                    Allergens = allergens,
                    ImageData = _selectedImageData,
                    ImageFileName = _selectedImageFileName
                };

                // Disable save button during request
                SaveFoodButton.IsEnabled = false;
                SaveFoodButton.Content = "Saving...";

                // Create food
                var createdFood = await _api.CreateFoodAsync(request);

                MessageBox.Show("Food created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Close dialog
                DialogOverlay.Visibility = Visibility.Collapsed;

                // Reload foods list
                await LoadFoods();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create food: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveFoodButton.IsEnabled = true;
                SaveFoodButton.Content = "Save Food";
            }
        }

        private void CancelDialog_Click(object sender, RoutedEventArgs e)
        {
            DialogOverlay.Visibility = Visibility.Collapsed;
        }

        private async void DeleteFood_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is long foodId)
                {
                    var food = _foods.FirstOrDefault(f => f.Id == foodId);
                    if (food == null) return;

                    var result = MessageBox.Show(
                        $"Are you sure you want to delete '{food.Name}'?",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        await _api.DeleteFoodAsync(foodId);
                        MessageBox.Show("Food deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadFoods();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete food: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class FoodViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public string PictureId { get; set; }
        public List<Allergen> Allergens { get; set; }
        public string AllergensDisplay { get; set; }
    }
}