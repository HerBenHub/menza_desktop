using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using menza_admin.Models;

namespace menza_admin
{
    public partial class Orders : Page
    {
        private List<OrderDisplayItem> _orderItems = new List<OrderDisplayItem>();
        private DateTime _selectedDate;

        public Orders()
        {
            InitializeComponent();
            Loaded += Orders_Loaded;
        }

        private async void Orders_Loaded(object sender, RoutedEventArgs e)
        {
            // Set today's date as default
            _selectedDate = DateTime.Now;
            OrderDatePicker.SelectedDate = _selectedDate;
            
            await LoadOrdersForDate(_selectedDate);
        }

        private async void OrderDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrderDatePicker.SelectedDate.HasValue)
            {
                _selectedDate = OrderDatePicker.SelectedDate.Value;
                await LoadOrdersForDate(_selectedDate);
            }
        }

        private async System.Threading.Tasks.Task LoadOrdersForDate(DateTime date)
        {
            try
            {
                // Show loading state
                ShowLoadingState();

                // Update date display
                DateDisplay.Text = date.ToString("dddd, MMMM d, yyyy");

                // Get ISO week number
                int weekNumber = GetIso8601WeekOfYear(date);
                int dayOfWeek = GetIsoDayOfWeek(date);

                // Fetch orders from API
                var orders = await App.Api.GetOrdersByWeekAsync(date.Year, weekNumber, dayOfWeek);

                // Process orders for display
                _orderItems = orders.Select(order => new OrderDisplayItem
                {
                    Id = order.Id,
                    Name = order.Name,
                    Price = order.Price,
                    PriceFormatted = $"{order.Price} Ft",
                    TodayQuantity = order.Days.ContainsKey(dayOfWeek) ? order.Days[dayOfWeek] : 0,
                    TodayRevenue = order.Days.ContainsKey(dayOfWeek) 
                        ? order.Days[dayOfWeek] * order.Price 
                        : 0
                }).ToList();

                // Calculate total revenue
                int totalRevenue = _orderItems.Sum(item => item.TodayRevenue);
                int totalOrders = _orderItems.Sum(item => item.TodayQuantity);

                // Update UI
                OrdersDataGrid.ItemsSource = _orderItems;
                TotalRevenueText.Text = $"{totalRevenue:N0} Ft";
                TotalOrdersText.Text = totalOrders.ToString();

                // Show appropriate state
                if (_orderItems.Count == 0)
                {
                    ShowEmptyState();
                }
                else
                {
                    ShowDataState();
                }
            }
            catch (Exception ex)
            {
                ShowErrorState($"Error loading orders: {ex.Message}");
            }
        }

        private void ShowLoadingState()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            EmptyPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            OrdersDataGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowEmptyState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            OrdersDataGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowErrorState(string message)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            OrdersDataGrid.Visibility = Visibility.Collapsed;
            ErrorText.Text = message;
        }

        private void ShowDataState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            OrdersDataGrid.Visibility = Visibility.Visible;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadOrdersForDate(_selectedDate);
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_orderItems.Count == 0)
            {
                MessageBox.Show("No orders to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Create CSV content
                var csv = "Food Name,Price,Quantity,Total Revenue\n";
                foreach (var item in _orderItems)
                {
                    csv += $"\"{item.Name}\",{item.Price},{item.TodayQuantity},{item.TodayRevenue}\n";
                }

                // Save to file
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Orders_{_selectedDate:yyyy-MM-dd}",
                    DefaultExt = ".csv",
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(dialog.FileName, csv);
                    MessageBox.Show("Export completed successfully!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper method to get ISO 8601 week number
        private int GetIso8601WeekOfYear(DateTime date)
        {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }

            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        // Helper method to get ISO day of week (Monday = 1, Sunday = 7)
        private int GetIsoDayOfWeek(DateTime date)
        {
            int dayOfWeek = (int)date.DayOfWeek;
            return dayOfWeek == 0 ? 7 : dayOfWeek; // Convert Sunday from 0 to 7
        }
    }

    // Display model for DataGrid
    public class OrderDisplayItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public string PriceFormatted { get; set; }
        public int TodayQuantity { get; set; }
        public int TodayRevenue { get; set; }
        public string TodayRevenueFormatted => $"{TodayRevenue:N0} Ft";
    }
}