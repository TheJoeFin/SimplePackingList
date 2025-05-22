using Microsoft.Windows.ApplicationModel.Resources;
using SimplePackingList.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SimplePackingList.Services;

public class GooglePlacesService : IPlacesService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string AutocompleteBaseUrl = "https://maps.googleapis.com/maps/api/place/autocomplete/json";
    private const string DetailsBaseUrl = "https://maps.googleapis.com/maps/api/place/details/json";

    public GooglePlacesService()
    {
        _httpClient = new HttpClient();

        // Load API key from resources
        ResourceManager resourceManager = new();
        ResourceMap resourceMap = resourceManager.MainResourceMap.GetSubtree("ApiKeys");
        ResourceContext resourceContext = resourceManager.CreateResourceContext();
        _apiKey = resourceMap.GetValue("GooglePlacesApiKey", resourceContext).ValueAsString;

        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("Google Places API key not found in resources");
    }

    public async Task<List<PlacePrediction>> GetPlaceSuggestionsAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            List<PlacePrediction> suggestions = [];

            // Build the request URL
            string requestUrl = $"{AutocompleteBaseUrl}?input={Uri.EscapeDataString(query)}&types=geocode&key={_apiKey}";

            // Make the API request
            PlacesApiResponse? response = await _httpClient.GetFromJsonAsync<PlacesApiResponse>(requestUrl, cancellationToken);

            if (response is null || response.Status is not "OK")
                return suggestions;

            // For each prediction, get its full details including coordinates
            foreach (PlacePrediction prediction in response.Predictions)
            {
                // Get place details to retrieve coordinates
                if (string.IsNullOrEmpty(prediction.PlaceId))
                    continue;

                try
                {
                    PlaceDetailsResponse? placeDetails = await GetPlaceDetailsAsync(prediction.PlaceId, cancellationToken);
                    if (placeDetails is not null && placeDetails.Result is not null)
                    {
                        prediction.Latitude = placeDetails.Result.Geometry?.Location?.Lat ?? 0;
                        prediction.Longitude = placeDetails.Result.Geometry?.Location?.Lng ?? 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting place details: {ex.Message}");
                }
                
                suggestions.Add(prediction);
            }

            return suggestions;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting place suggestions: {ex.Message}");
            return [];
        }
    }

    private async Task<PlaceDetailsResponse?> GetPlaceDetailsAsync(string placeId, CancellationToken cancellationToken)
    {
        string requestUrl = $"{DetailsBaseUrl}?place_id={placeId}&fields=geometry&key={_apiKey}";
        return await _httpClient.GetFromJsonAsync<PlaceDetailsResponse>(requestUrl, cancellationToken);
    }

    // Classes for deserializing the Google Places API response
    private class PlacesApiResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("predictions")]
        public List<PlacePrediction> Predictions { get; set; } = [];
    }

    // Classes for deserializing the Place Details API response
    private class PlaceDetailsResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("result")]
        public PlaceResult? Result { get; set; }
    }

    private class PlaceResult
    {
        [JsonPropertyName("geometry")]
        public Geometry? Geometry { get; set; }
    }

    private class Geometry
    {
        [JsonPropertyName("location")]
        public Location? Location { get; set; }
    }

    private class Location
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }
}
