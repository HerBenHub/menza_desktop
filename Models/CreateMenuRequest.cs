using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace menza_admin.Models
{
    public class CreateMenuRequest
    {
        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("week")]
        public int Week { get; set; }

        [JsonPropertyName("days")]
        public Dictionary<string, List<string>> Days { get; set; }
    }
}