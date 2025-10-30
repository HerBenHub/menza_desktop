using System.Text.Json.Serialization;

namespace menza_admin.Models
{
    public class CreateMenuResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
//This file may not exist in the future because it is only used for the response of CreateMenu API call.