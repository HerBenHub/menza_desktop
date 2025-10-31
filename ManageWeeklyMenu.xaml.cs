using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using menza_admin.Models;

namespace menza_admin
{
    public partial class ManageWeeklyMenu : Page
    {
        private Dictionary<int, List<Food>> _weeklyMenu = new Dictionary<int, List<Food>>
        {
            { 1, new List<Food>() }, // Monday
            { 2, new List<Food>() }, // Tuesday
            { 3, new List<Food>() }, // Wednesday
            { 4, new List<Food>() }, // Thursday
            { 5, new List<Food>() }  // Friday
        };

        private List<Food> _allFoods = new List<Food>();

        public ManageWeeklyMenu()
        {
            InitializeComponent();
            Loaded += ManageWeeklyMenu_Loaded;
        }

        private async void ManageWeeklyMenu_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAllFoods();
        }

        private async Task LoadAllFoods()
        {
            try
            {
                var response = await App.Api.GetAsync("/v1/food");
                _allFoods = System.Text.Json.JsonSerializer.Deserialize<List<Food>>(
                    response, Services.Api.JsonOptions) ?? new List<Food>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load foods: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(YearTextBox.Text, out int year) || 
                !int.TryParse(WeekTextBox.Text, out int week))
            {
                MessageBox.Show("Kérem adjon meg érvényes évet és hetet!", "Hiba", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                StatusText.Text = "Betöltés...";
                var menus = await App.Api.GetMenuAsync(week, year);

                // Clear existing menu
                foreach (var day in _weeklyMenu.Keys.ToList())
                {
                    _weeklyMenu[day].Clear();
                }

                // Load menu data for each day
                foreach (var menu in menus.Where(m => m.Day >= 1 && m.Day <= 5))
                {
                    if (menu.Foods != null)
                    {
                        _weeklyMenu[menu.Day] = new List<Food>(menu.Foods);
                    }
                }

                RefreshAllDays();
                StatusText.Text = "Menü betöltve!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nem sikerült betölteni a menüt: {ex.Message}", "Hiba", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "";
            }
        }

        private void NewMenu_Click(object sender, RoutedEventArgs e)
        {
            foreach (var day in _weeklyMenu.Keys.ToList())
            {
                _weeklyMenu[day].Clear();
            }
            RefreshAllDays();
            StatusText.Text = "Új menü létrehozva";
        }

        private void AddFood_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            int day = int.Parse(button.Tag.ToString());

            // Check if already has 3 foods
            if (_weeklyMenu[day].Count >= 3)
            {
                MessageBox.Show("Egy napra maximum 3 ételt lehet hozzáadni!", "Figyelmeztetés", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Show food selection dialog
            var dialog = new FoodSelectionDialog(_allFoods, _weeklyMenu[day]);
            if (dialog.ShowDialog() == true && dialog.SelectedFood != null)
            {
                _weeklyMenu[day].Add(dialog.SelectedFood);
                RefreshDay(day);
                StatusText.Text = "";
            }
        }

        private void RemoveFood_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            int day = int.Parse(button.Tag.ToString());
            long foodId = (long)button.CommandParameter;

            var food = _weeklyMenu[day].FirstOrDefault(f => f.Id == foodId);
            if (food != null)
            {
                _weeklyMenu[day].Remove(food);
                RefreshDay(day);
                StatusText.Text = "";
            }
        }

        private async void SaveMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(YearTextBox.Text, out int year) || 
                !int.TryParse(WeekTextBox.Text, out int week))
            {
                MessageBox.Show("Kérem adjon meg érvényes évet és hetet!", "Hiba", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate that all days have foods
            var emptyDays = _weeklyMenu.Where(kvp => kvp.Value.Count == 0).ToList();
            if (emptyDays.Any())
            {
                var result = MessageBox.Show(
                    "Néhány nap még üres. Biztos, hogy menteni szeretné?", 
                    "Figyelmeztetés", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                
                if (result != MessageBoxResult.Yes)
                    return;
            }

            try
            {
                StatusText.Text = "Mentés...";

                var request = new CreateMenuRequest
                {
                    Year = year,
                    Week = week,
                    Days = new Dictionary<string, List<string>>()
                };

                // Convert food lists to ID lists
                foreach (var day in _weeklyMenu)
                {
                    request.Days[day.Key.ToString()] = day.Value.Select(f => f.Id.ToString()).ToList();
                }

                var response = await App.Api.CreateMenuAsync(request);
                
                StatusText.Text = "Menü sikeresen mentve!";
                MessageBox.Show("A heti menü sikeresen mentve!", "Siker", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = "";
                MessageBox.Show($"Nem sikerült menteni a menüt: {ex.Message}", "Hiba", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshAllDays()
        {
            RefreshDay(1); // Monday
            RefreshDay(2); // Tuesday
            RefreshDay(3); // Wednesday
            RefreshDay(4); // Thursday
            RefreshDay(5); // Friday
        }

        private void RefreshDay(int day)
        {
            ItemsControl control = null;
            switch (day)
            {
                case 1:
                    control = MondayItems;
                    break;
                case 2:
                    control = TuesdayItems;
                    break;
                case 3:
                    control = WednesdayItems;
                    break;
                case 4:
                    control = ThursdayItems;
                    break;
                case 5:
                    control = FridayItems;
                    break;
            }

            if (control != null)
            {
                control.ItemsSource = null;
                control.ItemsSource = _weeklyMenu[day];
            }
        }
    }
}