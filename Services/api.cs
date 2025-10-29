using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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

            var food = JsonSerializer.Deserialize<Food>(content);
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

                var food = JsonSerializer.Deserialize<Food>(responseContent);
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
    }
}
