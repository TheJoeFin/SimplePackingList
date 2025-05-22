namespace SimplePackingList.Models;

public record WeatherData
{
    public int dt { get; set; }
    public int sunrise { get; set; }
    public int sunset { get; set; }
    public float temp { get; set; }
    public float feels_like { get; set; }
    public int pressure { get; set; }
    public int humidity { get; set; }
    public float dew_point { get; set; }
    public float uvi { get; set; }
    public int clouds { get; set; }
    public int visibility { get; set; }
    public float wind_speed { get; set; }
    public int wind_deg { get; set; }
    public WeatherCondition[] weather { get; set; } = [];
}