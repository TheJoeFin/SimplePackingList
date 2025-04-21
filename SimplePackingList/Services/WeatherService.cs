using Microsoft.Windows.ApplicationModel.Resources;
using SimplePackingList.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SimplePackingList.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string OneCallApiUrl = "https://api.openweathermap.org/data/3.0/onecall";

    public WeatherService()
    {
        _httpClient = new HttpClient();

        // Load API key from resources
        ResourceManager resourceManager = new();
        ResourceMap resourceMap = resourceManager.MainResourceMap.GetSubtree("ApiKeys");
        ResourceContext resourceContext = resourceManager.CreateResourceContext();
        _apiKey = resourceMap.GetValue("OpenWeatherApiKey", resourceContext).ValueAsString;

        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("OpenWeather API key not found in resources");
    }

    public async Task<WeatherInfo?> GetWeatherForecastAsync(double latitude, double longitude, DateTimeOffset date, CancellationToken cancellationToken)
    {
        try
        {
            // Check if the date is within 5 days (OpenWeather forecast limitation)
            TimeSpan timeDifference = date - DateTimeOffset.Now;
            if (timeDifference.TotalDays > 5)
            {
                System.Diagnostics.Debug.WriteLine("Date is more than 5 days ahead - forecast data not available");
                return null;
            }

            string weatherUnits = RegionInfo.CurrentRegion.IsMetric ? "metric" : "imperial";

            // Convert date to Unix timestamp in seconds for OneCall API
            long timestamp = date.ToUnixTimeSeconds();

            // OneCall API requires these parameters
            string requestUrl = $"{OneCallApiUrl}?lat={latitude}&lon={longitude}&units={weatherUnits}&dt={timestamp}&appid={_apiKey}";

            Rootobject? response = await _httpClient.GetFromJsonAsync<Rootobject>(requestUrl, cancellationToken);
            // Rootobject? response = null;
            // JsonDocument? jsonDoc = await _httpClient.GetFromJsonAsync<JsonDocument>(requestUrl, cancellationToken);

            if (response is null)
                return null;

            // Process the returned data for the requested date
            // The OneCall v3 API with timestamp returns data for the specific day requested

            // If we have the data for the requested day in the response
            if (response.data != null && response.data.Length > 0)
            {
                var weatherData = response.data[0];

                bool hasPrecipitation = false;
                string condition = string.Empty;
                string description = string.Empty;
                string icon = string.Empty;

                if (weatherData.weather.Length > 0)
                {
                    condition = weatherData.weather[0].main;
                    description = weatherData.weather[0].description;
                    icon = weatherData.weather[0].icon;

                    // Check for precipitation
                    hasPrecipitation =
                        condition.Contains("Rain", StringComparison.OrdinalIgnoreCase) ||
                        condition.Contains("Snow", StringComparison.OrdinalIgnoreCase) ||
                        condition.Contains("Drizzle", StringComparison.OrdinalIgnoreCase);
                }

                return new WeatherInfo
                {
                    Temperature = weatherData.temp,
                    Condition = condition,
                    Description = description,
                    Humidity = weatherData.humidity,
                    HasPrecipitation = hasPrecipitation,
                    ForecastDate = DateTimeOffset.FromUnixTimeSeconds(weatherData.dt),
                    Icon = icon
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting weather forecast: {ex.Message}");
            return null;
        }
    }

    #region API Response Classes
    // OneCall API v3 response structure
    private class OneCallResponse
    {
        [JsonPropertyName("data")]
        public List<WeatherDataItem> Data { get; set; } = [];
    }


    public class Rootobject
    {
        public float lat { get; set; }
        public float lon { get; set; }
        public string timezone { get; set; }
        public int timezone_offset { get; set; }
        public Datum[] data { get; set; }
    }

    public class Datum
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
        public Weather[] weather { get; set; }
    }

    public class Weather
    {
        public int id { get; set; }
        public string main { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
    }


    private class WeatherDataItem
    {
        [JsonPropertyName("dt")]
        public long Dt { get; set; }

        [JsonPropertyName("temp")]
        public double Temp { get; set; }

        [JsonPropertyName("min_temp")]
        public double MinTemp { get; set; }

        [JsonPropertyName("max_temp")]
        public double MaxTemp { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }

        [JsonPropertyName("weather")]
        public List<WeatherCondition> Weather { get; set; } = [];

        [JsonPropertyName("clouds")]
        public int Clouds { get; set; }

        [JsonPropertyName("wind_speed")]
        public double WindSpeed { get; set; }

        [JsonPropertyName("wind_deg")]
        public int WindDeg { get; set; }
    }

    private class WeatherCondition
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("main")]
        public string Main { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;
    }
    #endregion
}
