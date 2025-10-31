using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using menza_admin.Models;

namespace menza_admin
{
    public partial class ManageWeeklyMenu : Page
    {
        // Store 3 foods per day (days 1-5 for Monday-Friday)
        private Dictionary<int, Food[]> _weeklyMenu = new Dictionary<int, Food[]>
        {
            { 1, new Food[3] }, // Monday
            { 2, new Food[3] }, // Tuesday
            { 3, new Food[3] }, // Wednesday
            { 4, new Food[3] }, // Thursday
            { 5, new Food[3] }  // Friday
        };

        private List<Food> _allFoods = new List<Food>();

        public ManageWeeklyMenu()
        {
            InitializeComponent();
            Loaded += ManageWeeklyMenu_Loaded;
        }

        private async void ManageWeeklyMenu_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeYearComboBox();
            await LoadAllFoods();
            RefreshAllSlots();
        }

        private void InitializeYearComboBox()
        {
            int currentYear = DateTime.Now.Year;
            var years = new List<int>();
            
            // Add previous year, current year, and next year
            for (int i = currentYear - 1; i <= currentYear + 1; i++)
            {
                years.Add(i);
            }
            
            YearComboBox.ItemsSource = years;
            YearComboBox.SelectedItem = currentYear;
        }

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (YearComboBox.SelectedItem != null)
            {
                int selectedYear = (int)YearComboBox.SelectedItem;
                InitializeWeekComboBox(selectedYear);
            }
        }

        private void InitializeWeekComboBox(int year)
        {
            var weeks = new List<WeekInfo>();
            
            // Get the number of ISO 8601 weeks in the year
            int weeksInYear = GetWeeksInYear(year);
            
            for (int weekNum = 1; weekNum <= weeksInYear; weekNum++)
            {
                var weekInfo = GetWeekInfo(year, weekNum);
                weeks.Add(weekInfo);
            }
            
            WeekComboBox.ItemsSource = weeks;
                
            // Select current week if it's the current year
            if (year == DateTime.Now.Year)
            {
                int currentWeek = GetIso8601WeekOfYear(DateTime.Now);
                var currentWeekInfo = weeks.FirstOrDefault(w => w.WeekNumber == currentWeek);
                if (currentWeekInfo != null)
                {
                    WeekComboBox.SelectedItem = currentWeekInfo;
                }
            }
            else if (weeks.Count > 0)
            {
                WeekComboBox.SelectedIndex = 0;
            }
        }

        private WeekInfo GetWeekInfo(int year, int weekNumber)
        {
            // Find the first Monday of the week
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
            if (daysOffset < 0)
                daysOffset += 7;
            
            DateTime firstMonday = jan1.AddDays(daysOffset);
            
            // If the first Monday is in the previous year's last week, adjust
            if (GetIso8601WeekOfYear(firstMonday) != 1)
            {
                firstMonday = firstMonday.AddDays(7);
            }
            
            DateTime weekStart = firstMonday.AddDays((weekNumber - 1) * 7);
            DateTime weekEnd = weekStart.AddDays(4); // Friday
            
            return new WeekInfo
            {
                WeekNumber = weekNumber,
                Year = year,
                StartDate = weekStart,
                EndDate = weekEnd,
                DateRange = $"{weekStart:MMM dd} - {weekEnd:MMM dd}"
            };
        }

        private int GetWeeksInYear(int year)
        {
            DateTime dec31 = new DateTime(year, 12, 31);
            int week = GetIso8601WeekOfYear(dec31);
            
            // If Dec 31 is in week 1, it belongs to next year
            if (week == 1)
            {
                dec31 = dec31.AddDays(-7);
                week = GetIso8601WeekOfYear(dec31);
            }
            
            return week;
        }

        private int GetIso8601WeekOfYear(DateTime date)
        {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }

            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        private async Task LoadAllFoods()
        {
            try
            {
                _allFoods = await App.Api.GetAllFoodsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nem sikerült betölteni az ételeket: {ex.Message}", "Hiba", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadMenu_Click(object sender, RoutedEventArgs e)
        {
            if (YearComboBox.SelectedItem == null || WeekComboBox.SelectedItem == null)
            {
                MessageBox.Show("Kérem válasszon évet és hetet!", "Hiba", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int year = (int)YearComboBox.SelectedItem;
            var weekInfo = (WeekInfo)WeekComboBox.SelectedItem;
            int week = weekInfo.WeekNumber;

            try
            {
                StatusText.Text = "Betöltés...";
                var menus = await App.Api.GetMenuAsync(week, year);

                // Clear existing menu
                foreach (var day in _weeklyMenu.Keys.ToList())
                {
                    _weeklyMenu[day] = new Food[3];
                }

                // Load menu data for each day
                foreach (var menu in menus.Where(m => m.Day >= 1 && m.Day <= 5))
                {
                    if (menu.Foods != null && menu.Foods.Count > 0)
                    {
                        for (int i = 0; i < Math.Min(menu.Foods.Count, 3); i++)
                        {
                            _weeklyMenu[menu.Day][i] = menu.Foods[i];
                        }
                    }
                }

                RefreshAllSlots();
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
                _weeklyMenu[day] = new Food[3];
            }
            RefreshAllSlots();
            StatusText.Text = "Új menü létrehozva";
        }

        private List<Food> GetUsedFoodsForDay(int day)
        {
            return _weeklyMenu[day].Where(f => f != null).ToList();
        }

        private async void SaveMenu_Click(object sender, RoutedEventArgs e)
        {
            if (YearComboBox.SelectedItem == null || WeekComboBox.SelectedItem == null)
            {
                MessageBox.Show("Kérem válasszon évet és hetet!", "Hiba", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int year = (int)YearComboBox.SelectedItem;
            var weekInfo = (WeekInfo)WeekComboBox.SelectedItem;
            int week = weekInfo.WeekNumber;

            // Validate that all days have exactly 3 foods
            foreach (var day in _weeklyMenu)
            {
                if (day.Value.Any(f => f == null))
                {
                    MessageBox.Show($"Minden napra pontosan 3 ételt kell választani! Hiányzó étel: {GetDayName(day.Key)}", 
                        "Hiányos adatok", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
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

                // Convert food arrays to ID lists (exactly 3 per day)
                foreach (var day in _weeklyMenu)
                {
                    request.Days[day.Key.ToString()] = day.Value
                        .Select(f => f.Id.ToString())
                        .ToList();
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

        private void RefreshAllSlots()
        {
            for (int day = 1; day <= 5; day++)
            {
                for (int slot = 0; slot < 3; slot++)
                {
                    RefreshSlot(day, slot);
                }
            }
        }

        private void RefreshSlot(int day, int slot)
        {
            ContentControl control = GetSlotControl(day, slot);
            if (control == null) return;

            var food = _weeklyMenu[day][slot];
            
            if (food == null)
            {
                // Empty slot - show dropdown
                control.Content = CreateFoodDropdown(day, slot);
            }
            else
            {
                // Food selected - show food info only
                control.Content = CreateFoodDisplayCard(food, day, slot);
            }
        }

        private ContentControl GetSlotControl(int day, int slot)
        {
            string controlName = $"{GetDayNameEnglish(day)}{slot + 1}";
            return FindName(controlName) as ContentControl;
        }

        private DataTemplate CreateFoodItemTemplate()
        {
            // Create a DataTemplate for the ComboBox items
            var template = new DataTemplate();
            
            var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            
            var nameTextFactory = new FrameworkElementFactory(typeof(TextBlock));
            nameTextFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));
            nameTextFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            nameTextFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 10, 0));
            
            var priceTextFactory = new FrameworkElementFactory(typeof(TextBlock));
            priceTextFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Price") 
            { 
                StringFormat = "- {0} Ft" 
            });
            priceTextFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(76, 175, 80)));
            
            stackPanelFactory.AppendChild(nameTextFactory);
            stackPanelFactory.AppendChild(priceTextFactory);
            
            template.VisualTree = stackPanelFactory;
            
            return template;
        }

        private UIElement CreateFoodDropdown(int day, int slot)
        {
            var comboBox = new ComboBox
            {
                ItemsSource = _allFoods,
                ItemTemplate = CreateFoodItemTemplate(),
                MinHeight = 80,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(10),
                FontSize = 14
            };

            // Set placeholder text
            var placeholderText = new TextBlock
            {
                Text = "Válasszon ételt...",
                Foreground = new SolidColorBrush(Colors.Gray),
                FontStyle = FontStyles.Italic,
                IsHitTestVisible = false,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Create a grid to overlay placeholder
            var grid = new Grid();
            grid.Children.Add(comboBox);
            grid.Children.Add(placeholderText);

            // Hide placeholder when item is selected or dropdown is opened
            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem != null)
                {
                    placeholderText.Visibility = Visibility.Collapsed;
                    var selectedFood = comboBox.SelectedItem as Food;
                    if (selectedFood != null)
                    {
                        _weeklyMenu[day][slot] = selectedFood;
                        RefreshSlot(day, slot);
                        StatusText.Text = "";
                    }
                }
                else
                {
                    placeholderText.Visibility = Visibility.Visible;
                }
            };

            comboBox.DropDownOpened += (s, e) =>
            {
                placeholderText.Visibility = Visibility.Collapsed;
            };

            comboBox.DropDownClosed += (s, e) =>
            {
                if (comboBox.SelectedItem == null)
                {
                    placeholderText.Visibility = Visibility.Visible;
                }
            };

            return grid;
        }

        private UIElement CreateFoodDisplayCard(Food food, int day, int slot)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(10),
                MinHeight = 80,
                Cursor = Cursors.Hand
            };

            var stackPanel = new StackPanel();

            var nameText = new TextBlock
            {
                Text = food.Name,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var priceText = new TextBlock
            {
                Text = $"{food.Price} Ft",
                Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                FontWeight = FontWeights.Bold
            };

            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(priceText);
            border.Child = stackPanel;

            // Click to change food
            border.MouseLeftButtonDown += (s, e) =>
            {
                // Change back to dropdown to allow selection
                _weeklyMenu[day][slot] = null;
                RefreshSlot(day, slot);
                StatusText.Text = "";
            };

            return border;
        }

        private string GetDayName(int day)
        {
            if (day == 1) return "Hétfő";
            if (day == 2) return "Kedd";
            if (day == 3) return "Szerda";
            if (day == 4) return "Csütörtök";
            if (day == 5) return "Péntek";
            return "";
        }

        private string GetDayNameEnglish(int day)
        {
            if (day == 1) return "Monday";
            if (day == 2) return "Tuesday";
            if (day == 3) return "Wednesday";
            if (day == 4) return "Thursday";
            if (day == 5) return "Friday";
            return "";
        }
    }

    // Helper class for week information
    public class WeekInfo
    {
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string DateRange { get; set; }
        public string DisplayText => $"{WeekNumber}. hét - {DateRange}";
    }
}