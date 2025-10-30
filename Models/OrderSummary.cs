using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace menza_admin.Models
{
    public class OrderSummary
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("price")]
        public int Price { get; set; }

        [JsonPropertyName("days")]
        public Dictionary<int, int> Days { get; set; } = new Dictionary<int, int>();
    }
}