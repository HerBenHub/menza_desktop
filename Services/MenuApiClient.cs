using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace menza_admin.Services
{
    private readonly HttpClient _httpClient = new HttpClient();

    public MainWindow()
    {
        InitializeComponent();
        _httpClient.BaseAddress = new Uri("http://localhost:3001");
    }
    //public class MenuApiClient
    //{
    //    private readonly HttpClient _httpClient;

    //    public MenuApiClient()
    //    {
    //        _httpClient = new HttpClient
    //        {
    //            BaseAddress = new Uri("https://localhost:3001/v1/menu")
    //        };
    //    }

    //    public async Task<JsonElement?> GetMenuAsync(int week, int? year = null)
    //    {
    //        try
    //        {
    //            string url = $"menu?week={week}";
    //            if (year.HasValue)
    //                url += $"&year={year.Value}";

    //            var response = await _httpClient.GetAsync(url);
    //            if (!response.IsSuccessStatusCode)
    //                return null;

    //            var json = await response.Content.ReadAsStringAsync();
    //            return JsonSerializer.Deserialize<JsonElement>(json);
    //        }
    //        catch
    //        {
    //            return null;
    //        }
    //    }
    //}

    private async AddFood()
    {
        var newFood = new
        {
            name = "Teszter étel",
            description = "Frissen sült csirkecomb párolt rizzsel és savanyúsággal",
            price = 1890,
            vatRate = 27,
            stripeTaxCode = "txcd_99999999",
            pictureId = "placeholder_img_123"
        };

        try
        {
            string json = JsonConvert.SerializeObject(newFood);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("/v1/food", content);

            string responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show($"Sikeresen hozzáadva!\nVálasz: {responseText}");
            }
            else
            {
                MessageBox.Show($"Hiba: {response.StatusCode}\n{responseText}");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hiba a kapcsolat során:\n{ex.Message}");
        }
    }
}
