using System;

namespace SimplePackingList.Models;

public class WeatherInfo
{
    public double Temperature { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double MinTemperature { get; set; }
    public double MaxTemperature { get; set; }
    public int Humidity { get; set; }
    public bool HasPrecipitation { get; set; }
    public DateTimeOffset ForecastDate { get; set; }
    public string Icon { get; set; } = string.Empty;

    public bool IsHot => MaxTemperature > 80;
    public bool IsCold => MinTemperature < 50;
    public bool IsRainy => Condition.Contains("rain", StringComparison.OrdinalIgnoreCase) || 
                          Description.Contains("rain", StringComparison.OrdinalIgnoreCase);
    public bool IsSnowy => Condition.Contains("snow", StringComparison.OrdinalIgnoreCase) || 
                          Description.Contains("snow", StringComparison.OrdinalIgnoreCase);
    public bool IsWindy => Description.Contains("wind", StringComparison.OrdinalIgnoreCase) || 
                           Description.Contains("breez", StringComparison.OrdinalIgnoreCase);

    public string Main { get; internal set; } = string.Empty;

    public override string ToString()
    {
        return $"{ForecastDate:MMM dd}: {Condition}, {Temperature:F1}°F";
    }
}
