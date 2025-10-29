using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace menza_admin.Models
{
    //Itt azok a mezõk vannak amelyek szükségesek egy új étel létrehozásához
    public class Food
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public int Price { get; set; }

        [JsonPropertyName("pictureId")]
        public string PictureId { get; set; } = string.Empty;

        [JsonPropertyName("allergens")]
        public List<Allergen> Allergens { get; set; } = new List<Allergen>();

        [JsonPropertyName("vatRate")]
        public int VatRate { get; set; } = 27;

        [JsonPropertyName("stripeTaxCode")]
        public string StripeTaxCode { get; set; } = "txcd_99999999";

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("vatAmount")]
        public int VatAmount { get; set; }

        [JsonPropertyName("priceWithoutVat")]
        public int PriceWithoutVat { get; set; }

        [JsonPropertyName("menus")]
        public List<MenuRef> Menus { get; set; } = new List<MenuRef>();
    }

    public class Allergen
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class MenuRef
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
    }
}