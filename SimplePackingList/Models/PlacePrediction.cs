using System.Text.Json.Serialization;

namespace SimplePackingList.Models;

public class PlacePrediction
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; } = "";

    // These properties don't come directly from the API but are populated after getting place details
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public override string ToString()
    {
        return Description;
    }
}
