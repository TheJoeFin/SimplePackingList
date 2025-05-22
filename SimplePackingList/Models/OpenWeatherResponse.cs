namespace SimplePackingList.Models;

public record OpenWeatherResponse
{
    // Use lower case property names to match OpenWeatherMap JSON response
    public float lat { get; set; }
    public float lon { get; set; }
    public string timezone { get; set; } = string.Empty;
    public int timezone_offset { get; set; }
    public WeatherData[] data { get; set; } = [];
}
