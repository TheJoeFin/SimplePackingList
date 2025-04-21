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
    private const string OneCallApiUrl = "https://api.openweathermap.org/data/3.0/onecall/timemachine";

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

            // For debugging
            System.Diagnostics.Debug.WriteLine($"Making API request to: {requestUrl}");

            // Deserialize the response
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false // OpenWeatherMap uses lowercase property names
            };

            OpenWeatherResponse? response = await _httpClient.GetFromJsonAsync<OpenWeatherResponse>(requestUrl, options, cancellationToken);

            if (response is null)
            {
                System.Diagnostics.Debug.WriteLine("API response was null");
                return null;
            }

            // Process the returned data for the requested date
            // The OneCall v3 API with timestamp returns data for the specific day requested

            // If we have the data for the requested day in the response
            if (response.data != null && response.data.Length > 0)
            {
                WeatherData weatherData = response.data[0];
                System.Diagnostics.Debug.WriteLine($"Received weather data: {JsonSerializer.Serialize(weatherData)}");

                bool hasPrecipitation = false;
                string condition = string.Empty;
                string description = string.Empty;
                string icon = string.Empty;

                if (weatherData.weather != null && weatherData.weather.Length > 0)
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
            else
            {
                System.Diagnostics.Debug.WriteLine("No data element in the response");
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
    // OpenWeatherMap OneCall API v3 response structure
    public class OpenWeatherResponse
    {
        // Use lower case property names to match OpenWeatherMap JSON response
        public float lat { get; set; }
        public float lon { get; set; }
        public string timezone { get; set; } = string.Empty;
        public int timezone_offset { get; set; }
        public WeatherData[] data { get; set; } = [];
    }

    public class WeatherData
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

    public class WeatherCondition
    {
        public int id { get; set; }
        public string main { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public string icon { get; set; } = string.Empty;
    }
    #endregion
}
