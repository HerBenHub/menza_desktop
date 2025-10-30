using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace menza_admin.Models
{
    public class Menu
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("week")]
        public int Week { get; set; }

        [JsonPropertyName("day")]
        public int Day { get; set; }

        [JsonPropertyName("foods")]
        public List<Food> Foods { get; set; } = new List<Food>();
    }
}