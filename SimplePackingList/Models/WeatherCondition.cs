namespace SimplePackingList.Models;

public record WeatherCondition
{
    public int id { get; set; }
    public string main { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string icon { get; set; } = string.Empty;
}
