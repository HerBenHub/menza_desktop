using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using menza_admin.Models;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Grid;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Drawing;
using iTextSharp.text.pdf;
using PdfPage = iTextSharp.text.pdf.PdfPage;
using iTextSharp.text;

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

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_orderItems.Count == 0)
            {
                MessageBox.Show("No orders to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                ExportButton.IsEnabled = false;
                ErrorText.Text = "";

                var saveDialog = new SaveFileDialog
                {
                    FileName = $"Napi_Összesítés_{_selectedDate:yyyy-MM-dd}",
                    DefaultExt = ".pdf",
                    Filter = "PDF files (*.pdf)|*.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var document = new Syncfusion.Pdf.PdfDocument())
                    {
                        Syncfusion.Pdf.PdfPage page = document.Pages.Add();
                        PdfGraphics graphics = page.Graphics;

                        // Create fonts
                        Syncfusion.Pdf.Graphics.PdfFont titleFont = new PdfStandardFont(PdfFontFamily.TimesRoman, 18, PdfFontStyle.Bold);
                        Syncfusion.Pdf.Graphics.PdfFont normalFont = new PdfStandardFont(PdfFontFamily.TimesRoman, 12);

                        // Add title
                        graphics.DrawString($"Napi összesítés - {_selectedDate:yyyy.MM.dd}",
                            titleFont, PdfBrushes.Black, new PointF(0, 0));

                        // Create orders grid
                        PdfGrid grid = new PdfGrid();
                        grid.Columns.Add(4);
                        grid.Headers.Add(1);

                        // Style the grid
                        grid.Style.Font = normalFont;
                        
                        // Set header values
                        PdfGridRow header = grid.Headers[0];
                        header.Cells[0].Value = "Étel neve";
                        header.Cells[1].Value = "Ár (Ft)";
                        header.Cells[2].Value = "Mennyiség";
                        header.Cells[3].Value = "Összesen (Ft)";

                        // Add data rows
                        foreach (var order in _orderItems)
                        {
                            PdfGridRow row = grid.Rows.Add();
                            row.Cells[0].Value = order.Name ?? "Unknown";
                            row.Cells[1].Value = order.Price.ToString();
                            row.Cells[2].Value = order.TodayQuantity.ToString();
                            row.Cells[3].Value = order.TodayRevenue.ToString();
                        }

                        // Calculate totals
                        int totalQuantity = _orderItems.Sum(x => x.TodayQuantity);
                        int totalRevenue = _orderItems.Sum(x => x.TodayRevenue);

                        // Draw the grid
                        grid.Draw(page, new RectangleF(0, 50, page.Size.Width, 500));

                        // Add totals at the bottom
                        float yPos = 570; // 50 (grid Y) + 500 (grid height) + 20 (spacing)
                        graphics.DrawString($"Összes rendelés: {totalQuantity}", 
                            titleFont, PdfBrushes.Black, new PointF(0, yPos));
                        graphics.DrawString($"Összes bevétel: {totalRevenue:N0} Ft", 
                            titleFont, PdfBrushes.Black, new PointF(0, yPos + 30));

                        // Save the document
                        using (var stream = saveDialog.OpenFile())
                        {
                            document.Save(stream);
                        }
                    }

                    MessageBox.Show("PDF sikeresen elkészítve!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Export failed: {ex.Message}";
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                ExportButton.IsEnabled = true;
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

        // Helper method to handle encoding of text in PDF cells
        private void AddCellWithEncoding(PdfPTable table, string text)
        {
            // Create a cell with proper encoding for Hungarian characters
            var cell = new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA, BaseFont.IDENTITY_H, true, 12)));
            table.AddCell(cell);
        }

        // Helper method to add cell with specific font
        private void AddCellWithFont(iTextSharp.text.pdf.PdfPTable table, string text, iTextSharp.text.Font font)
        {
            var cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(text, font));
            cell.Padding = 5;
            table.AddCell(cell);
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