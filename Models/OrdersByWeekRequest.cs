using System.Text.Json.Serialization;

namespace menza_admin.Models
{
    public class OrdersByWeekRequest
    {
        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("week")]
        public int Week { get; set; }

        [JsonPropertyName("day")]
        public int? Day { get; set; }
    }
}