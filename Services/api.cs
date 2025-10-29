using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using menza_admin.Models;

namespace menza_admin.Services
{
    public class api
    {
        private readonly HttpClient _client;

        public api(string baseUrl)
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
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/v1/food", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to create food. Status: {response.StatusCode}, Response: {responseContent}");
            }

            var food = JsonSerializer.Deserialize<Food>(responseContent);
            return food ?? throw new Exception("Failed to deserialize response");
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
    }
}
