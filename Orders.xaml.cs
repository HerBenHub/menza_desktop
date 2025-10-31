using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Syncfusion.Pdf.Grid;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Drawing;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace menza_admin
{
    /// <summary>
    /// Rendelések oldal osztály
    /// Megjeleníti és kezeli a napi rendeléseket, lehetővé teszi a szűrést és PDF exportálást
    /// </summary>
    public partial class Orders : Page
    {
        private List<OrderDisplayItem> _orderItems = new List<OrderDisplayItem>(); // Megjelenítendő rendelések listája
        private DateTime _selectedDate; // Kiválasztott dátum

        public Orders()
        {
            InitializeComponent();              
            Loaded += Orders_Loaded;
        }

        /// <summary>
        /// Oldal betöltésekor fut le
        /// Beállítja a mai dátumot alapértelmezettnek és betölti a rendeléseket
        /// </summary>
        private async void Orders_Loaded(object sender, RoutedEventArgs e)
        {
            // Mai dátum beállítása alapértelmezettként
            _selectedDate = DateTime.Now;
            OrderDatePicker.SelectedDate = _selectedDate;
            
            await LoadOrdersForDate(_selectedDate);
        }

        /// <summary>
        /// Eseménykezelő a dátumválasztó változásához
        /// Betölti a kiválasztott dátumhoz tartozó rendeléseket
        /// </summary>
        private async void OrderDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrderDatePicker.SelectedDate.HasValue)
            {
                _selectedDate = OrderDatePicker.SelectedDate.Value;
                await LoadOrdersForDate(_selectedDate);
            }
        }

        /// <summary>
        /// Betölti a megadott dátumhoz tartozó rendeléseket az API-ból
        /// Kiszámítja az összesítéseket és frissíti a felhasználói felületet
        /// </summary>
        /// <param name="date">A lekérdezni kívánt dátum</param>
        private async System.Threading.Tasks.Task LoadOrdersForDate(DateTime date)
        {
            try
            {
                // Betöltési állapot megjelenítése
                ShowLoadingState();

                // Dátum megjelenítésének frissítése
                DateDisplay.Text = date.ToString("dddd, MMMM d, yyyy");

                // ISO hétszám lekérése
                int weekNumber = GetIso8601WeekOfYear(date);
                int dayOfWeek = GetIsoDayOfWeek(date);

                // Rendelések lekérése az API-ból
                var orders = await App.Api.GetOrdersByWeekAsync(date.Year, weekNumber, dayOfWeek);

                // Rendelések feldolgozása megjelenítéshez
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

                // Teljes bevétel számítása
                int totalRevenue = _orderItems.Sum(item => item.TodayRevenue);
                int totalOrders = _orderItems.Sum(item => item.TodayQuantity);

                // Felület frissítése
                OrdersDataGrid.ItemsSource = _orderItems;
                TotalRevenueText.Text = $"{totalRevenue:N0} Ft";
                TotalOrdersText.Text = totalOrders.ToString();

                // Megfelelő állapot megjelenítése
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
                ShowErrorState($"Hiba a rendelések betöltése során: {ex.Message}");
            }
        }

        /// <summary>
        /// Megjeleníti a betöltési állapotot
        /// </summary>
        private void ShowLoadingState()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            EmptyPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            OrdersDataGrid.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Megjeleníti az üres állapotot (nincs rendelés)
        /// </summary>
        private void ShowEmptyState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            OrdersDataGrid.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Megjeleníti a hibaállapotot
        /// </summary>
        /// <param name="message">Hibaüzenet</param>
        private void ShowErrorState(string message)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            OrdersDataGrid.Visibility = Visibility.Collapsed;
            ErrorText.Text = message;
        }

        /// <summary>
        /// Megjeleníti az adatok állapotát
        /// </summary>
        private void ShowDataState()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            OrdersDataGrid.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Eseménykezelő a Frissítés gomb kattintásához
        /// Újratölti a kiválasztott dátumhoz tartozó rendeléseket
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadOrdersForDate(_selectedDate);
        }

        /// <summary>
        /// Eseménykezelő a PDF Export gomb kattintásához
        /// Létrehoz egy PDF dokumentumot a napi rendelésekkel és összesítésekkel
        /// </summary>
        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_orderItems.Count == 0)
            {
                MessageBox.Show("Nincs exportálható rendelés.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    Filter = "PDF fájlok (*.pdf)|*.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var document = new Syncfusion.Pdf.PdfDocument())
                    {
                        Syncfusion.Pdf.PdfPage page = document.Pages.Add();
                        PdfGraphics graphics = page.Graphics;

                        // Betűtípusok létrehozása
                        Syncfusion.Pdf.Graphics.PdfFont titleFont = new PdfStandardFont(PdfFontFamily.TimesRoman, 18, PdfFontStyle.Bold);
                        Syncfusion.Pdf.Graphics.PdfFont normalFont = new PdfStandardFont(PdfFontFamily.TimesRoman, 12);

                        // Cím hozzáadása
                        graphics.DrawString($"Napi összesítés - {_selectedDate:yyyy.MM.dd}",
                            titleFont, PdfBrushes.Black, new PointF(0, 0));

                        // Rendelések táblázat létrehozása
                        PdfGrid grid = new PdfGrid();
                        grid.Columns.Add(4);
                        grid.Headers.Add(1);

                        // Táblázat stílusának beállítása
                        grid.Style.Font = normalFont;
                        
                        // Fejléc értékek beállítása
                        PdfGridRow header = grid.Headers[0];
                        header.Cells[0].Value = "Étel neve";
                        header.Cells[1].Value = "Ár (Ft)";
                        header.Cells[2].Value = "Mennyiség";
                        header.Cells[3].Value = "Összesen (Ft)";

                        // Adatsorok hozzáadása
                        foreach (var order in _orderItems)
                        {
                            PdfGridRow row = grid.Rows.Add();
                            row.Cells[0].Value = order.Name ?? "Ismeretlen";
                            row.Cells[1].Value = order.Price.ToString();
                            row.Cells[2].Value = order.TodayQuantity.ToString();
                            row.Cells[3].Value = order.TodayRevenue.ToString();
                        }

                        // Összesítések számítása
                        int totalQuantity = _orderItems.Sum(x => x.TodayQuantity);
                        int totalRevenue = _orderItems.Sum(x => x.TodayRevenue);

                        // Táblázat rajzolása
                        grid.Draw(page, new RectangleF(0, 50, page.Size.Width, 500));

                        // Összesítések hozzáadása az alján
                        float yPos = 570;
                        graphics.DrawString($"Összes rendelés: {totalQuantity}", 
                            titleFont, PdfBrushes.Black, new PointF(0, yPos));
                        graphics.DrawString($"Összes bevétel: {totalRevenue:N0} Ft", 
                            titleFont, PdfBrushes.Black, new PointF(0, yPos + 30));

                        // Dokumentum mentése
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
                ErrorText.Text = $"Export sikertelen: {ex.Message}";
                MessageBox.Show($"Export sikertelen: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                ExportButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Segédmetódus az ISO 8601 hétszám lekéréséhez
        /// </summary>
        /// <param name="date">A dátum, amelyhez a hétszámot keressük</param>
        /// <returns>ISO 8601 hétszám</returns>
        private int GetIso8601WeekOfYear(DateTime date)
        {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }

            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        /// <summary>
        /// Segédmetódus az ISO hét napjának lekéréséhez (Hétfő = 1, Vasárnap = 7)
        /// </summary>
        /// <param name="date">A dátum</param>
        /// <returns>Nap sorszáma (1-7)</returns>
        private int GetIsoDayOfWeek(DateTime date)
        {
            int dayOfWeek = (int)date.DayOfWeek;
            return dayOfWeek == 0 ? 7 : dayOfWeek; // Vasárnap konvertálása 0-ról 7-re
        }

        /// <summary>
        /// Segédmetódus PDF cellák szövegkódolásának kezeléséhez
        /// </summary>
        private void AddCellWithEncoding(PdfPTable table, string text)
        {
            // Cella létrehozása megfelelő kódolással a magyar karakterekhez
            var cell = new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA, BaseFont.IDENTITY_H, true, 12)));
            table.AddCell(cell);
        }

        /// <summary>
        /// Segédmetódus cella hozzáadásához megadott betűtípussal
        /// </summary>
        private void AddCellWithFont(iTextSharp.text.pdf.PdfPTable table, string text, iTextSharp.text.Font font)
        {
            var cell = new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Phrase(text, font));
            cell.Padding = 5;
            table.AddCell(cell);
        }
    }

    /// <summary>
    /// Megjelenítési modell a DataGrid számára
    /// Reprezentál egy rendelési tételt a megjelenítéshez szükséges adatokkal
    /// </summary>
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