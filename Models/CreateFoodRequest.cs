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

        [JsonIgnore]
        public List<long> Allergens { get; set; } = new List<long>();

        [JsonPropertyName("allergens")]
        public List<string> GetAllergensAsStrings
        {
            get { return Allergens.ConvertAll(a => a.ToString()); }
            set { Allergens = value.ConvertAll(s => long.Parse(s)); }
        }

        [JsonPropertyName("vatRate")]
        public int VatRate { get; set; } = 27;

        [JsonPropertyName("stripeTaxCode")]
        public string StripeTaxCode { get; set; } = "txcd_99999999";
    }
}