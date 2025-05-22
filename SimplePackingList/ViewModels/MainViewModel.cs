using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SimplePackingList.Models;
using SimplePackingList.Services;
using SimplePackingList.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Networking.Connectivity;

namespace SimplePackingList.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // Debounce related fields
    private CancellationTokenSource? _placesSearchCts;
    private const int _debounceDelay = 300; // milliseconds
    private readonly ConnectionProfile? connectionProfile;
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly GooglePlacesService _placesService;
    private readonly WeatherService _weatherService;
    private CancellationTokenSource? _weatherCts;
    private readonly DispatcherTimer _llmTimer = new();
    private readonly DispatcherTimer _saveSettingsTimer = new();
    private readonly DispatcherTimer _propChangedTimer = new();

    private readonly string standardStuff = """
        - Toothbrush
        - Toothpaste
        - Deodorant
        - Hairbrush
        - Razor
        - Shampoo
        - Phone charger
        """;

    private int numberOfDays = 3;

    [ObservableProperty]
    public partial ObservableCollection<Trip> TripsList { get; set; }

    [ObservableProperty]
    public partial Trip CurrentTrip { get; set; }

    [ObservableProperty]
    public partial string ListTitle { get; set; } = "Simple Packing List";

    [ObservableProperty]
    public partial bool IsRunning { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSwimming { get; set; } = false;

    [ObservableProperty]
    public partial bool IsHiking { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSnowSport { get; set; } = false;

    [ObservableProperty]
    public partial bool IsGifting { get; set; } = false;

    [ObservableProperty]
    public partial int NumberOfFormalEvents { get; set; } = 0;

    [ObservableProperty]
    public partial bool HasLaundry { get; set; } = false;

    [ObservableProperty]
    public partial WeatherInfo? Weather { get; set; }

    [ObservableProperty]
    public partial string WeatherStatus { get; set; } = "No weather data available";

    [ObservableProperty]
    public partial bool IsLoadingWeather { get; set; } = false;

    // Place-related properties
    [ObservableProperty]
    public partial string Destination { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<PlacePrediction> PlaceSuggestions { get; set; } = [];

    [ObservableProperty]
    public partial bool IsShowingBotNotes { get; set; } = false;

    [ObservableProperty]
    public partial bool IsLoadingBotNotes { get; set; } = false;

    [ObservableProperty]
    public partial string BotNotes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Visibility CopyBotNotesVisible { get; set; } = Visibility.Collapsed;

    partial void OnIsRunningChanged(bool value)
    {
        UpdateDaysAndText();
    }

    partial void OnIsSwimmingChanged(bool value)
    {
        UpdateDaysAndText();
    }

    partial void OnIsHikingChanged(bool value)
    {
        UpdateDaysAndText();
    }

    partial void OnIsSnowSportChanged(bool value)
    {
        UpdateDaysAndText();
    }

    partial void OnIsGiftingChanged(bool value)
    {
        UpdateDaysAndText();
    }

    partial void OnNumberOfFormalEventsChanged(int value)
    {
        UpdateDaysAndText();
    }

    partial void OnHasLaundryChanged(bool value)
    {
        UpdateDaysAndText();
    }

    partial void OnIsLoadingBotNotesChanged(bool value)
    {
        if (value)
            CopyBotNotesVisible = Visibility.Collapsed;
        else
            CopyBotNotesVisible = Visibility.Visible;
    }

    partial void OnDestinationChanged(string oldValue, string newValue)
    {
        UpdateDaysAndText();
        if (!string.IsNullOrEmpty(newValue))
        {
            UpdateWeatherData();
        }
    }

    public MainViewModel()
    {
        // Get the dispatcher queue for the current thread
        connectionProfile = NetworkInformation.GetInternetConnectionProfile();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _placesService = new GooglePlacesService();
        _weatherService = new WeatherService();
        _llmTimer.Interval = TimeSpan.FromSeconds(1);
        _llmTimer.Tick += LlmTimer_Tick;

        _saveSettingsTimer.Interval = TimeSpan.FromMilliseconds(500);
        _saveSettingsTimer.Tick += SaveSettingsTimer_Tick;

        _propChangedTimer.Interval = TimeSpan.FromMilliseconds(400);
        _propChangedTimer.Tick += PropChangedTimer_Tick;
    }

    private void PropChangedTimer_Tick(object? sender, object e)
    {
        _saveSettingsTimer.Stop();

        UpdateDaysAndText();
        UpdateWeatherData();

        _saveSettingsTimer.Start();
    }

    private void SaveSettingsTimer_Tick(object? sender, object e)
    {
        _saveSettingsTimer.Stop();

        LocalSettingsService.SaveSettingAsync(SettingKeys.LastTrip.ToString(), CurrentTrip);
    }

    public async Task LoadState()
    {
        Trip lastTrip = LocalSettingsService.ReadSettingAsync<Trip>(SettingKeys.LastTrip.ToString());
        if (lastTrip is not null)
            CurrentTrip = lastTrip;
        else
            CurrentTrip = Trip.NewTrip();

        CurrentTrip.PropertyChanging += CurrentTrip_PropertyChanging;
    }

    private void CurrentTrip_PropertyChanging(object? sender, System.ComponentModel.PropertyChangingEventArgs e)
    {
        _propChangedTimer.Stop();
        _propChangedTimer.Start();
    }

    private async void LlmTimer_Tick(object? sender, object e)
    {
        if (!WcrUtilities.HasNpu() || !WcrUtilities.DoesWindowsSupportAI())
            return;

        IsLoadingBotNotes = true;
        IsShowingBotNotes = true;
        BotNotes = "";
        _llmTimer.Stop();
        Progress<string> progress = new();

        string prompt = $"""
            {Weather}
            """;
        string wcrPrompt = await Singleton<WcrService>.Instance.TextResponseWithProgress(prompt, progress);

        // remove empty lines and spaces before hyphens
        wcrPrompt = wcrPrompt.Replace($"{Environment.NewLine}{Environment.NewLine}", Environment.NewLine);
        wcrPrompt = wcrPrompt.Replace(" -", "-");

        BotNotes = wcrPrompt.Trim();
        IsLoadingBotNotes = false;
    }

    [RelayCommand]
    public void CopyToClipboard()
    {
        DataPackage textPackage = new();
        textPackage.SetText(CurrentTrip.PackingText);

        ClipboardContentOptions options = new();
        options.HistoryFormats.Add(StandardDataFormats.Text);
        options.IsRoamable = true;
        options.RoamingFormats.Add(StandardDataFormats.Text);

        Clipboard.SetContentWithOptions(textPackage, options);
    }

    [RelayCommand]
    public void CopyToPackingList()
    {
        CurrentTrip.PackingText += Environment.NewLine;
        CurrentTrip.PackingText += Environment.NewLine;
        CurrentTrip.PackingText += BotNotes;
    }

    public void SearchPlaces(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is string chosenSelection)
        {
            // User selected a suggestion
            Destination = chosenSelection;
        }
        else if (!string.IsNullOrEmpty(args.QueryText))
        {
            // User pressed Enter without selecting a suggestion
            Destination = args.QueryText;
        }
    }

    // Method triggered when the text changes in the PlacesSearchBox
    public async void OnPlacesTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Only get results when it's user typing
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            string input = sender.Text.ToLower();
            if (string.IsNullOrEmpty(input))
            {
                PlaceSuggestions.Clear();
                return;
            }

            // Cancel any previous search task
            _placesSearchCts?.Cancel();
            _placesSearchCts = new CancellationTokenSource();

            // Start a new debounced search task
            await DebounceSearchPlacesAsync(input, _placesSearchCts.Token);
        }
    }

    private async Task DebounceSearchPlacesAsync(string searchText, CancellationToken cancellationToken)
    {
        if (connectionProfile is null)
            return;

        NetworkConnectivityLevel connectionLevel = connectionProfile.GetNetworkConnectivityLevel();

        if (connectionLevel == NetworkConnectivityLevel.None)
            return;

        try
        {
            // Wait for the debounce delay
            await Task.Delay(_debounceDelay, cancellationToken);

            // If the operation was cancelled, don't proceed
            cancellationToken.ThrowIfCancellationRequested();

            // Call the Places API service
            List<PlacePrediction> suggestions = await _placesService.GetPlaceSuggestionsAsync(searchText, cancellationToken);

            // Update the UI on the UI thread
            _dispatcherQueue?.TryEnqueue(() =>
            {
                PlaceSuggestions.Clear();
                foreach (PlacePrediction suggestion in suggestions)
                {
                    PlaceSuggestions.Add(suggestion);
                }
            });
        }
        catch (OperationCanceledException)
        {
            // Search was cancelled, do nothing
        }
        catch (Exception ex)
        {
            // Log the exception in a real app
            System.Diagnostics.Debug.WriteLine($"Error in place search: {ex.Message}");
        }
    }

    public void OnPlaceSelected(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string selectedPlace)
        {
            Destination = selectedPlace;
        }
    }

    private async void UpdateWeatherData()
    {
        if (connectionProfile is null)
            return;

        NetworkConnectivityLevel connectionLevel = connectionProfile.GetNetworkConnectivityLevel();
        if (connectionLevel == NetworkConnectivityLevel.None || CurrentTrip.StartDate is null || string.IsNullOrEmpty(Destination))
        {
            Weather = null;
            WeatherStatus = "Set both destination and dates to see weather forecast";
            return;
        }

        // Find the selected place in suggestions
        PlacePrediction? selectedPlace = PlaceSuggestions.FirstOrDefault(p => p.Description == Destination);
        if (selectedPlace == null || (selectedPlace.Latitude == 0 && selectedPlace.Longitude == 0))
        {
            WeatherStatus = "Location coordinates not available for weather forecast";
            return;
        }

        // check if start day is more than 4 days ahead
        TimeSpan timeDifference = CurrentTrip.StartDate.Value - DateTimeOffset.Now;
        if (timeDifference.TotalDays > 5)
        {
            WeatherStatus = "Weather forecast is limited to 5 days ahead";
            return;
        }

        try
        {
            // Cancel any previous operation
            _weatherCts?.Cancel();
            _weatherCts = new CancellationTokenSource();

            IsLoadingWeather = true;
            WeatherStatus = "Loading weather forecast...";

            WeatherInfo? weatherInfo = await _weatherService.GetWeatherForecastAsync(
                selectedPlace.Latitude, 
                selectedPlace.Longitude,
                CurrentTrip.StartDate.Value,
                _weatherCts.Token);

            _dispatcherQueue?.TryEnqueue(() =>
            {
                Weather = weatherInfo;
                WeatherStatus = weatherInfo != null 
                    ? $"Weather forecast: {weatherInfo}" 
                    : "Weather forecast unavailable for selected date (limited to 5 days ahead)";
                IsLoadingWeather = false;
                UpdateDaysAndText(); // Update packing list with weather recommendations
            });
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled, do nothing
        }
        catch (Exception ex)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                WeatherStatus = $"Error loading weather: {ex.Message}";
                IsLoadingWeather = false;
            });
        }
    }

    private void UpdateDaysAndText()
    {
        if (CurrentTrip.EndDate is null || CurrentTrip.StartDate is null)
            return;

        numberOfDays = (int)(CurrentTrip.EndDate.Value - CurrentTrip.StartDate.Value).TotalDays;
        numberOfDays = Math.Abs(numberOfDays);

        // Include destination in the packing list title if available
        CurrentTrip.PackingText = string.IsNullOrEmpty(Destination)
            ? $"Packing List for {CurrentTrip.StartDate:MMM dd} - {CurrentTrip.EndDate:MMM dd} for {numberOfDays} nights"
            : $"Packing List for {Destination} from {CurrentTrip.StartDate:MMM dd} - {CurrentTrip.EndDate:MMM dd} for {numberOfDays} nights";

        CurrentTrip.PackingText += Environment.NewLine + standardStuff;

        int effectiveDays = numberOfDays;

        if (HasLaundry)
            effectiveDays = Math.Min(7, numberOfDays);

        string clothes = $"""

            - {effectiveDays} shirts
            - {Math.Ceiling(effectiveDays / 3.0)} pair(s) of pants
            - {effectiveDays} pair(s) of socks
            - {effectiveDays} pair(s) of underwear
            """;

        CurrentTrip.PackingText += Environment.NewLine + clothes;

        if (IsRunning)
        {
            int numberOfRuns = (int)Math.Ceiling(effectiveDays / 3.0);
            CurrentTrip.PackingText += Environment.NewLine + $"""

                - running shoes
                - {numberOfRuns} running shirt(s)
                - {numberOfRuns} running short(s)
                - {numberOfRuns} running sock(s)
                - {numberOfRuns} running underwear

                """;
        }

        if (IsSwimming)
        {
            CurrentTrip.PackingText += Environment.NewLine + "- Swimsuit";
        }

        if (IsHiking)
        {
            CurrentTrip.PackingText += Environment.NewLine + "- Hiking boots";
        }

        if (IsSnowSport)
        {
            CurrentTrip.PackingText += Environment.NewLine + "- Snow boots";
        }

        if (IsGifting)
        {
            CurrentTrip.PackingText += Environment.NewLine + "- Gift";
        }

        if (NumberOfFormalEvents > 0)
        {
            CurrentTrip.PackingText += Environment.NewLine + $"- {NumberOfFormalEvents} Formal outfit(s)";
        }

        // Add weather-based recommendations if available
        if (Weather is not null)
        {
            _llmTimer.Stop();
            _llmTimer.Start();
            // List<string> weatherRecommendations = [];
            
            //if (Weather?.IsRainy is true)
            //{
            //    weatherRecommendations.Add("- Umbrella");
            //    weatherRecommendations.Add("- Raincoat or waterproof jacket");
            //    weatherRecommendations.Add("- Waterproof shoes");
            //}
            
            //if (Weather?.IsSnowy is true)
            //{
            //    weatherRecommendations.Add("- Heavy winter coat");
            //    weatherRecommendations.Add("- Snow boots");
            //    weatherRecommendations.Add("- Gloves");
            //    weatherRecommendations.Add("- Winter hat");
            //    weatherRecommendations.Add("- Scarf");
            //    weatherRecommendations.Add("- Thermal underwear");
            //}
            
            //if (Weather?.IsCold is true)
            //{
            //    weatherRecommendations.Add("- Warm jacket");
            //    weatherRecommendations.Add("- Long sleeve shirts");
            //    weatherRecommendations.Add("- Sweater or hoodie");
            //}
            
            //if (Weather?.IsHot is true)
            //{
            //    weatherRecommendations.Add("- Sunscreen");
            //    weatherRecommendations.Add("- Hat or cap");
            //    weatherRecommendations.Add("- Sunglasses");
            //    weatherRecommendations.Add("- Light, breathable clothing");
            //    weatherRecommendations.Add("- Water bottle");
            //}
            
            //if (Weather?.IsWindy is true)
            //{
            //    weatherRecommendations.Add("- Windbreaker jacket");
            //}
            
            //if (weatherRecommendations.Count > 0)
            //{
            //    PackingText += Environment.NewLine + Environment.NewLine + $"Weather recommendations ({Weather?.ForecastDate:MMM dd}, {Weather?.Condition}):" + Environment.NewLine;
            //    PackingText += string.Join(Environment.NewLine, weatherRecommendations);
            //}
        }
    }
}
