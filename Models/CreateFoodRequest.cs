using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace menza_admin.Models
{
    //Itt azok a mezõk vannak amelyek szükségesek egy új étel létrehozásához, amikor POST kérést küldünk
    public class CreateFoodRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public int Price { get; set; }

        [JsonPropertyName("pictureId")]
        public string PictureId { get; set; } = string.Empty;

        [JsonPropertyName("allergens")]
        public List<string> Allergens { get; set; } = new List<string>();

        // New property to handle the image file
        [JsonIgnore]
        public byte[] ImageData { get; set; }

        [JsonIgnore]
        public string ImageFileName { get; set; }
    }
}