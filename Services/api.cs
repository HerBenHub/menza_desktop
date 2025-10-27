using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace menza_admin.Services
{
    public class api
    {
        private readonly HttpClient _client;

        public api(string baseUrl)
        {
            _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<string> GetAsync(string endpoint)
        {
            var response = await _client.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        internal async Task<JsonDocument> GetUserAsync()
        {
            throw new NotImplementedException();
        }
    }
}
