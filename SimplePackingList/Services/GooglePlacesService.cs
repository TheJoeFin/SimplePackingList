using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Windows.ApplicationModel.Resources;

namespace SimplePackingList.Services;

public class GooglePlacesService : IPlacesService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://maps.googleapis.com/maps/api/place/autocomplete/json";

    public GooglePlacesService()
    {
        _httpClient = new HttpClient();

        // Load API key from resources
        ResourceManager resourceManager = new();
        ResourceMap resourceMap = resourceManager.MainResourceMap.GetSubtree("ApiKeys");
        ResourceContext resourceContext = resourceManager.CreateResourceContext();
        _apiKey = resourceMap.GetValue("GooglePlacesApiKey", resourceContext).ValueAsString;
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException("Google Places API key not found in resources");
        }
    }

    public async Task<List<string>> GetPlaceSuggestionsAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var suggestions = new List<string>();
            
            // Build the request URL
            var requestUrl = $"{BaseUrl}?input={Uri.EscapeDataString(query)}&types=geocode&key={_apiKey}";
            
            // Make the API request
            var response = await _httpClient.GetFromJsonAsync<PlacesApiResponse>(requestUrl, cancellationToken);
            
            if (response is null || response.Status != "OK")
            {
                // Handle error or no results
                return suggestions;
            }
            
            // Extract and return the place descriptions
            foreach (var prediction in response.Predictions)
            {
                suggestions.Add(prediction.Description);
            }
            
            return suggestions;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting place suggestions: {ex.Message}");
            return new List<string>();
        }
    }
    
    // Classes for deserializing the Google Places API response
    private class PlacesApiResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";
        
        [JsonPropertyName("predictions")]
        public List<PlacePrediction> Predictions { get; set; } = new();
    }
    
    private class PlacePrediction
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }
}
