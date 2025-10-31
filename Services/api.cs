using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using menza_admin.Models;

//Ez a file úgymond az összekötő file
//Itt nincsenek konrét adaok csak metódusok amelyekkel az API-t elérjük

namespace menza_admin.Services
{
    public class Api : IDisposable
    {
        private readonly HttpClient _client;
        private bool disposed = false;

        // Static JsonSerializerOptions with converters for all API calls
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            Converters = { new BigIntConverter(), new FlexibleDateTimeConverter() }
        };

        public Api(string baseUrl)
        {
            _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _client.DefaultRequestHeaders.Add("X-Client-Type", "desktop");
            _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> GetAsync(string endpoint)
        {
            var response = await _client.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<Food> GetFoodByIdAsync(string id)
        {
            var response = await _client.GetAsync($"/v1/food/{id}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get food with ID {id}. Status: {response.StatusCode}, Response: {content}");
            }

            var food = JsonSerializer.Deserialize<Food>(content, JsonOptions);
            return food ?? throw new Exception($"Food with ID {id} not found");
        }

        public async Task<Food> CreateFoodAsync(CreateFoodRequest request)
        {
            using (var multipartContent = new MultipartFormDataContent())
            {
                // Add the JSON data
                var jsonData = JsonSerializer.Serialize(new
                {
                    name = request.Name,
                    description = request.Description,
                    price = request.Price,
                    allergens = request.Allergens
                });
                multipartContent.Add(new StringContent(jsonData), "data");

                // Add the image file if present
                if (request.ImageData != null)
                {
                    var imageContent = new ByteArrayContent(request.ImageData);
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    multipartContent.Add(imageContent, "file", request.ImageFileName);
                }

                var response = await _client.PostAsync("/v1/food", multipartContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to create food. Status: {response.StatusCode}, Response: {responseContent}");
                }

                var food = JsonSerializer.Deserialize<Food>(responseContent, JsonOptions);
                return food ?? throw new Exception("Failed to deserialize response");
            }
        }

        //Del kaja
        public async Task DeleteFoodAsync(long id)
        {
            //A lekérdezett ételek közül választunk egyet a UI-on, majd annak az ID-ját használjuk a törléshez
            //Egyenlőre csak egy ételt lehet eltávolítani egyszerre
            var response = await _client.DeleteAsync($"/v1/food/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to delete food with ID {id}. Status: {response.StatusCode}, Response: {content}");
            }
        }

        public async Task<List<OrderSummary>> GetOrdersByWeekAsync(int year, int week, int? day = null)
        {
            var endpoint = $"/v1/order?year={year}&week={week}";
            if (day.HasValue)
            {
                endpoint += $"&day={day.Value}";
            }

            var response = await _client.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get orders. Status: {response.StatusCode}, Response: {content}");
            }

            var orders = JsonSerializer.Deserialize<List<OrderSummary>>(content, JsonOptions);
            return orders ?? new List<OrderSummary>();
        }

        // Optional: Add an overload that takes a request object
        public async Task<List<OrderSummary>> GetOrdersByWeekAsync(OrdersByWeekRequest request)
        {
            return await GetOrdersByWeekAsync(request.Year, request.Week, request.Day);
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, object data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _client.PostAsync(endpoint, content);
        }

        public void SetAuthToken(string token)
        {
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _client?.Dispose();
                }
                disposed = true;
            }
        }

        public async Task<List<Menu>> GetMenuAsync(int week, int? year = null)
        {
            var endpoint = $"/v1/menu?week={week}";
            if (year.HasValue)
            {
                endpoint += $"&year={year.Value}";
            }

            var response = await _client.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get menu. Status: {response.StatusCode}, Response: {content}");
            }

            var menu = JsonSerializer.Deserialize<List<Menu>>(content, JsonOptions);
            return menu ?? new List<Menu>();
        }

        //Create menu
        public async Task<CreateMenuResponse> CreateMenuAsync(CreateMenuRequest request)
        {
            var response = await PostAsync("/v1/menu", request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to create menu. Status: {response.StatusCode}, Response: {content}");
            }

            var result = JsonSerializer.Deserialize<CreateMenuResponse>(content, JsonOptions);
            return result ?? throw new Exception("Failed to deserialize response");
        }
    }

    // Custom JSON converter to handle BigInt values from Node.js API
    public class BigIntConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // Handle bigint as string (e.g., "123456789")
                if (long.TryParse(reader.GetString(), out long result))
                {
                    return result;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                // Handle regular number
                return reader.GetInt64();
            }

            throw new JsonException($"Unable to convert token type {reader.TokenType} to Int64");
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    // Custom JSON converter to handle DateTime values from Node.js API
    public class FlexibleDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var dateString = reader.GetString();
                    if (DateTime.TryParse(dateString, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.RoundtripKind, out DateTime stringResult))
                    {
                        return stringResult;
                    }
                    throw new JsonException($"Unable to parse DateTime from string: {dateString}");

                case JsonTokenType.Number:
                    // Handle Unix timestamp in milliseconds
                    var timestamp = reader.GetInt64();
                    
                    // Check if it's in milliseconds (larger number) or seconds
                    if (timestamp > 10000000000) // Timestamp is in milliseconds
                    {
                        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
                    }
                    else // Timestamp is in seconds
                    {
                        return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                    }

                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing DateTime");
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("O")); // ISO 8601 format
        }
    }
}


// Usage examples(GetOrdersByWeekAsync):

//// Using individual parameters
//var orders = await App.Api.GetOrdersByWeekAsync(2025, 45);

//// Using request object
//var request = new OrdersByWeekRequest 
//{
//    Year = 2025,
//    Week = 45,
//    Day = null  // Optional day filter
//};
//var orders = await App.Api.GetOrdersByWeekAsync(request);



 
//// Get menu for a specific week
//var weeklyMenu = await App.Api.GetMenuAsync(45, 2025);

//// Create a new menu
//var createMenuRequest = new CreateMenuRequest
//{
//    Year = 2025,
//    Week = 45,
//    Days = new Dictionary<string, List<string>>
//    {
//        ["1"] = new List<string> { "foodId1", "foodId2", "foodId3" },
//        ["2"] = new List<string> { "foodId4", "foodId5", "foodId6" },
//        // ... other days
//    }
//};

//var response = await App.Api.CreateMenuAsync(createMenuRequest);
