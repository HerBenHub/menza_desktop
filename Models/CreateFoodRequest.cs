using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace menza_admin.Models
{
    //Itt azok a mez�k vannak amelyek sz�ks�gesek egy �j �tel l�trehoz�s�hoz, amikor POST k�r�st k�ld�nk
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