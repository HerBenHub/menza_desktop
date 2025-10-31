using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using menza_admin.Models;

namespace menza_admin
{
    /// <summary>
    /// Converts a Food object to a BitmapImage by constructing the CDN URL.
    /// 
    /// What it does:
    /// 1. Takes a Food object with Id and PictureId properties
    /// 2. Builds CDN URL: https://cdn-canteen.kenderesi.hu/food/{foodId}/{pictureId}.webp
    /// 3. Creates a BitmapImage from that URL
    /// 4. Returns a placeholder if the image fails to load or PictureId is missing
    /// 
    /// Example:
    /// Food with Id=21936604 and PictureId="abc123" becomes:
    /// https://cdn-canteen.kenderesi.hu/food/21936604/abc123.webp
    /// </summary>
    public class FoodImageConverter : IValueConverter
    {
        private const string CDN_BASE_URL = "https://cdn-canteen.kenderesi.hu/food";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Food food && !string.IsNullOrEmpty(food.PictureId))
            {
                try
                {
                    // Build the CDN URL: https://cdn-canteen.kenderesi.hu/food/{foodId}/{pictureId}.webp
                    string imageUrl = $"{CDN_BASE_URL}/{food.Id}/{food.PictureId}.webp";
                    
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imageUrl);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 100; // Optimize for display size
                    bitmap.EndInit();
                    
                    return bitmap;
                }
                catch
                {
                    // Return placeholder image if loading fails
                    return null; // WPF will handle null gracefully
                }
            }

            // Return null if no picture ID (WPF will show empty image area)
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}