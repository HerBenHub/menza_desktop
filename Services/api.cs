using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using menza_admin.Models;

namespace menza_admin.Services
{
    /// <summary>
    /// API kliens osztály - összekötő réteg a backend API és az alkalmazás között
    /// Felelős az összes HTTP kérés kezeléséért és a válaszok feldolgozásáért
    /// </summary>
    public class Api : IDisposable
    {
        private readonly HttpClient _client;
        private bool disposed = false;

        /// <summary>
        /// Statikus JSON szerializálási beállítások az összes API híváshoz
        /// Tartalmazza a BigInt és DateTime konvertereket a Node.js API kompatibilitáshoz
        /// </summary>
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            Converters = { new BigIntConverter(), new FlexibleDateTimeConverter() }
        };

        /// <summary>
        /// Konstruktor - inicializálja az API klienst
        /// </summary>
        /// <param name="baseUrl">Az API alap URL-je (pl. http://localhost:3001)</param>
        public Api(string baseUrl)
        {
            _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
            _client.DefaultRequestHeaders.Add("X-Client-Type", "desktop");
            _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        #region Általános HTTP metódusok

        /// <summary>
        /// Általános GET kérés végrehajtása
        /// </summary>
        /// <param name="endpoint">Az API végpont relatív útvonala</param>
        /// <returns>A válasz szöveges tartalma</returns>
        public async Task<string> GetAsync(string endpoint)
        {
            var response = await _client.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Általános POST kérés végrehajtása JSON tartalommal
        /// </summary>
        /// <param name="endpoint">Az API végpont relatív útvonala</param>
        /// <param name="data">Az elküldendő adat objektum</param>
        /// <returns>HTTP válasz üzenet</returns>
        public async Task<HttpResponseMessage> PostAsync(string endpoint, object data)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _client.PostAsync(endpoint, content);
        }

        /// <summary>
        /// Beállítja az autentikációs tokent a kérésekhez
        /// </summary>
        /// <param name="token">Bearer token</param>
        public void SetAuthToken(string token)
        {
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        #endregion

        #region Étel (Food) műveletek

        /// <summary>
        /// Lekér egy adott ételt ID alapján
        /// </summary>
        /// <param name="id">Az étel azonosítója</param>
        /// <returns>Az étel objektum</returns>
        /// <exception cref="Exception">Ha nem található az étel vagy sikertelen a lekérés</exception>
        public async Task<Food> GetFoodByIdAsync(string id)
        {
            var response = await _client.GetAsync($"/v1/food/{id}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Sikertelen étel lekérés ID-vel: {id}. Státusz: {response.StatusCode}, Válasz: {content}");
            }

            var food = JsonSerializer.Deserialize<Food>(content, JsonOptions);
            return food ?? throw new Exception($"Nem található étel ID-vel: {id}");
        }

        /// <summary>
        /// Lekéri az összes elérhető ételt
        /// </summary>
        /// <returns>Ételek listája</returns>
        /// <exception cref="Exception">Ha sikertelen a lekérés</exception>
        public async Task<List<Food>> GetAllFoodsAsync()
        {
            var response = await _client.GetAsync("/v1/food");
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Sikertelen ételek lekérése. Státusz: {response.StatusCode}, Válasz: {content}");
            }

            var foods = JsonSerializer.Deserialize<List<Food>>(content, JsonOptions);
            return foods ?? new List<Food>();
        }

        /// <summary>
        /// Új étel létrehozása képfeltöltéssel
        /// Multipart/form-data formátumban küldi el az adatokat (JSON + kép)
        /// </summary>
        /// <param name="request">Az étel létrehozási kérés adatai</param>
        /// <returns>A létrehozott étel objektum</returns>
        /// <exception cref="Exception">Ha sikertelen a létrehozás</exception>
        public async Task<Food> CreateFoodAsync(CreateFoodRequest request)
        {
            using (var multipartContent = new MultipartFormDataContent())
            {
                // JSON adatok hozzáadása
                var jsonData = JsonSerializer.Serialize(new
                {
                    name = request.Name,
                    description = request.Description,
                    price = request.Price,
                    allergens = request.Allergens
                });
                multipartContent.Add(new StringContent(jsonData), "data");

                // Képfájl hozzáadása, ha van
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
                    throw new Exception($"Sikertelen étel létrehozás. Státusz: {response.StatusCode}, Válasz: {responseContent}");
                }

                var food = JsonSerializer.Deserialize<Food>(responseContent, JsonOptions);
                return food ?? throw new Exception("Sikertelen válasz deszerializálás");
            }
        }

        /// <summary>
        /// Töröl egy ételt az adatbázisból ID alapján
        /// A felhasználói felületen kiválasztott étel ID-ját használja a törléshez
        /// Jelenleg csak egy ételt lehet törölni egyszerre
        /// </summary>
        /// <param name="id">A törlendő étel azonosítója</param>
        /// <exception cref="Exception">Ha sikertelen a törlés</exception>
        public async Task DeleteFoodAsync(long id)
        {
            var response = await _client.DeleteAsync($"/v1/food/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception($"Sikertelen étel törlés ID-vel: {id}. Státusz: {response.StatusCode}, Válasz: {content}");
            }
        }

        #endregion

        #region Rendelés (Order) műveletek

        /// <summary>
        /// Lekéri a rendeléseket hét és opcionálisan nap alapján
        /// </summary>
        /// <param name="year">Év</param>
        /// <param name="week">Hét száma (ISO 8601)</param>
        /// <param name="day">Nap száma (opcionális, 1-7)</param>
        /// <returns>Rendelés összesítések listája</returns>
        /// <exception cref="Exception">Ha sikertelen a lekérés</exception>
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
                throw new Exception($"Sikertelen rendelések lekérése. Státusz: {response.StatusCode}, Válasz: {content}");
            }

            var orders = JsonSerializer.Deserialize<List<OrderSummary>>(content, JsonOptions);
            return orders ?? new List<OrderSummary>();
        }

        /// <summary>
        /// Lekéri a rendeléseket hét alapján (túlterhelt verzió kérés objektummal)
        /// </summary>
        /// <param name="request">Rendelés lekérési kérés objektum</param>
        /// <returns>Rendelés összesítések listája</returns>
        public async Task<List<OrderSummary>> GetOrdersByWeekAsync(OrdersByWeekRequest request)
        {
            return await GetOrdersByWeekAsync(request.Year, request.Week, request.Day);
        }

        #endregion

        #region Menü (Menu) műveletek

        /// <summary>
        /// Lekéri a heti menüt megadott hét és opcionális év alapján
        /// </summary>
        /// <param name="week">Hét száma (ISO 8601)</param>
        /// <param name="year">Év (opcionális, alapértelmezett: aktuális év)</param>
        /// <returns>Menü lista (napokra bontva)</returns>
        /// <exception cref="Exception">Ha sikertelen a lekérés</exception>
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
                throw new Exception($"Sikertelen menü lekérés. Státusz: {response.StatusCode}, Válasz: {content}");
            }

            var menu = JsonSerializer.Deserialize<List<Menu>>(content, JsonOptions);
            return menu ?? new List<Menu>();
        }

        /// <summary>
        /// Új heti menü létrehozása
        /// Minden napra 3 ételt kell hozzárendelni (1-5 nap, hétfő-péntek)
        /// </summary>
        /// <param name="request">Menü létrehozási kérés (év, hét, napok ételei)</param>
        /// <returns>Létrehozási válasz üzenet</returns>
        /// <exception cref="Exception">Ha sikertelen a létrehozás (pl. már létezik menü)</exception>
        public async Task<CreateMenuResponse> CreateMenuAsync(CreateMenuRequest request)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/v1/menu", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Sikertelen menü létrehozás. Státusz: {response.StatusCode}, Válasz: {responseContent}");
            }

            var result = JsonSerializer.Deserialize<CreateMenuResponse>(responseContent, JsonOptions);
            return result ?? throw new Exception("Sikertelen válasz deszerializálás");
        }

        /// <summary>
        /// Meglévő heti menü frissítése
        /// PATCH metódussal frissíti a megadott hét menüjét
        /// </summary>
        /// <param name="request">Menü frissítési kérés (év, hét, napok ételei)</param>
        /// <returns>Frissítési válasz üzenet</returns>
        /// <exception cref="Exception">Ha sikertelen a frissítés</exception>
        public async Task<CreateMenuResponse> UpdateMenuAsync(CreateMenuRequest request)
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var requestMessage = new HttpRequestMessage(new HttpMethod("PATCH"), "/v1/menu")
            {
                Content = content
            };
            var response = await _client.SendAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Sikertelen menü frissítés. Státusz: {response.StatusCode}, Válasz: {responseContent}");
            }

            var result = JsonSerializer.Deserialize<CreateMenuResponse>(responseContent, JsonOptions);
            return result ?? throw new Exception("Sikertelen válasz deszerializálás");
        }

        #endregion

        #region IDisposable implementáció

        /// <summary>
        /// Felszabadítja az erőforrásokat (HttpClient)
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Védett Dispose metódus a felszabadítási mintához
        /// </summary>
        /// <param name="disposing">True, ha a managed erőforrásokat is fel kell szabadítani</param>
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

        #endregion
    }

    #region JSON Konverterek

    /// <summary>
    /// Egyedi JSON konverter a Node.js API BigInt értékeinek kezelésére
    /// A Node.js bigint típusokat stringként küldi, ezt konvertálja C# long típusra
    /// </summary>
    public class BigIntConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // BigInt kezelése stringként (pl. "123456789")
                if (long.TryParse(reader.GetString(), out long result))
                {
                    return result;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                // Normál szám kezelése
                return reader.GetInt64();
            }

            throw new JsonException($"Nem konvertálható a(z) {reader.TokenType} token típus Int64-re");
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    /// <summary>
    /// Egyedi JSON konverter a Node.js API DateTime értékeinek kezelésére
    /// Támogatja az ISO 8601 string formátumot és a Unix timestamp-eket (ms és s)
    /// </summary>
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
                    throw new JsonException($"Nem értelmezhető DateTime string: {dateString}");

                case JsonTokenType.Number:
                    // Unix timestamp kezelése milliszekundumban
                    var timestamp = reader.GetInt64();
                    
                    // Ellenőrzés: milliszekundum (nagyobb szám) vagy másodperc
                    if (timestamp > 10000000000) // Timestamp milliszekundumban
                    {
                        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
                    }
                    else // Timestamp másodpercben
                    {
                        return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                    }

                default:
                    throw new JsonException($"Váratlan token típus: {reader.TokenType} DateTime értelmezéskor");
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("O")); // ISO 8601 formátum
        }
    }

    #endregion
}

#region Használati példák

// ===== RENDELÉSEK LEKÉRÉSE =====

// 1. Egyedi paraméterekkel
// var orders = await App.Api.GetOrdersByWeekAsync(2025, 45);

// 2. Kérés objektummal
// var request = new OrdersByWeekRequest 
// {
//     Year = 2025,
//     Week = 45,
//     Day = null  // Opcionális nap szűrő
// };
// var orders = await App.Api.GetOrdersByWeekAsync(request);


// ===== MENÜ MŰVELETEK =====

// 1. Menü lekérése adott hétre
// var weeklyMenu = await App.Api.GetMenuAsync(45, 2025);

// 2. Új menü létrehozása
// var createMenuRequest = new CreateMenuRequest
// {
//     Year = 2025,
//     Week = 45,
//     Days = new Dictionary<string, List<string>>
//     {
//         ["1"] = new List<string> { "foodId1", "foodId2", "foodId3" }, // Hétfő
//         ["2"] = new List<string> { "foodId4", "foodId5", "foodId6" }, // Kedd
//         ["3"] = new List<string> { "foodId7", "foodId8", "foodId9" }, // Szerda
//         ["4"] = new List<string> { "foodId10", "foodId11", "foodId12" }, // Csütörtök
//         ["5"] = new List<string> { "foodId13", "foodId14", "foodId15" }  // Péntek
//     }
// };
// var response = await App.Api.CreateMenuAsync(createMenuRequest);

#endregion
