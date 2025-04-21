using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using SimplePackingList.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace SimplePackingList.ViewModels;
public partial class MainViewModel : ObservableObject
{
    // Debounce related fields
    private CancellationTokenSource? _placesSearchCts;
    private const int _debounceDelay = 500; // milliseconds
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly IPlacesService _placesService;

    private string standardStuff = """
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
    private string packingText = "PackingList";

    [ObservableProperty]
    private DateTimeOffset? startDate;

    [ObservableProperty]
    private DateTimeOffset? endDate;

    [ObservableProperty]
    private bool isRunning = false;

    [ObservableProperty]
    private bool isSwimming = false;

    [ObservableProperty]
    private bool isHiking = false;

    [ObservableProperty]
    private bool isSnowSport = false;

    [ObservableProperty]
    private bool isGifting = false;

    [ObservableProperty]
    private int numberOfFormalEvents = 0;

    [ObservableProperty]
    private bool hasLaundry = false;

    // Place-related properties
    [ObservableProperty]
    private string destination = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> placeSuggestions = new();

    partial void OnEndDateChanged(DateTimeOffset? oldValue, DateTimeOffset? newValue)
    {
        UpdateDaysAndText();
    }

    partial void OnStartDateChanged(DateTimeOffset? oldValue, DateTimeOffset? newValue)
    {
        UpdateDaysAndText();
    }

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

    partial void OnDestinationChanged(string oldValue, string newValue)
    {
        UpdateDaysAndText();
    }

    public MainViewModel()
    {
        // Get the dispatcher queue for the current thread
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _placesService = new GooglePlacesService();
    }

    [RelayCommand]
    public void CopyToClipboard()
    {
        DataPackage textPackage = new();
        textPackage.SetText(PackingText);

        ClipboardContentOptions options = new();
        options.HistoryFormats.Add(StandardDataFormats.Text);
        options.IsRoamable = true;
        options.RoamingFormats.Add(StandardDataFormats.Text);

        Clipboard.SetContentWithOptions(textPackage, options);
    }

    // Method triggered when the user submits a query in the PlacesSearchBox
    public void SearchPlaces(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is not null)
        {
            // User selected a suggestion
            Destination = args.ChosenSuggestion.ToString();
        }
        else if (!string.IsNullOrEmpty(args.QueryText))
        {
            // User pressed Enter without selecting a suggestion
            Destination = args.QueryText;
        }
    }

    // Method triggered when the text changes in the PlacesSearchBox
    public void OnPlacesTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
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
            DebounceSearchPlacesAsync(input, _placesSearchCts.Token);
        }
    }

    private async Task DebounceSearchPlacesAsync(string searchText, CancellationToken cancellationToken)
    {
        try
        {
            // Wait for the debounce delay
            await Task.Delay(_debounceDelay, cancellationToken);

            // If the operation was cancelled, don't proceed
            cancellationToken.ThrowIfCancellationRequested();

            // Call the Places API service
            var suggestions = await _placesService.GetPlaceSuggestionsAsync(searchText, cancellationToken);

            // Update the UI on the UI thread
            _dispatcherQueue?.TryEnqueue(() =>
            {
                PlaceSuggestions.Clear();
                foreach (string suggestion in suggestions)
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

    // Method triggered when a place suggestion is chosen
    public void OnPlaceSelected(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem != null)
        {
            Destination = args.SelectedItem.ToString();
        }
    }

    private void UpdateDaysAndText()
    {
        if (EndDate is null || StartDate is null)
            return;

        numberOfDays = (int)(EndDate.Value - StartDate.Value).TotalDays;
        numberOfDays = Math.Abs(numberOfDays);

        // Include destination in the packing list title if available
        PackingText = string.IsNullOrEmpty(Destination)
            ? $"Packing List for {StartDate:MMM dd} - {EndDate:MMM dd} for {numberOfDays} nights"
            : $"Packing List for {Destination} from {StartDate:MMM dd} - {EndDate:MMM dd} for {numberOfDays} nights";

        PackingText += Environment.NewLine + standardStuff;

        int effectiveDays = numberOfDays;

        if (HasLaundry)
            effectiveDays = Math.Min(7, numberOfDays);

        string clothes = $"""

            - {effectiveDays} shirts
            - {Math.Ceiling(effectiveDays / 3.0)} pair(s) of pants
            - {effectiveDays} pair(s) of socks
            - {effectiveDays} pair(s) of underwear
            """;

        PackingText += Environment.NewLine + clothes;

        if (IsRunning)
        {
            int numberOfRuns = (int)Math.Ceiling(effectiveDays / 3.0);
            PackingText += Environment.NewLine + $"""

                - running shoes
                - {numberOfRuns} running shirt(s)
                - {numberOfRuns} running short(s)
                - {numberOfRuns} running sock(s)
                - {numberOfRuns} running underwear

                """;
        }

        if (IsSwimming)
        {
            PackingText += Environment.NewLine + "- Swimsuit";
        }

        if (IsHiking)
        {
            PackingText += Environment.NewLine + "- Hiking boots";
        }

        if (IsSnowSport)
        {
            PackingText += Environment.NewLine + "- Snow boots";
        }

        if (IsGifting)
        {
            PackingText += Environment.NewLine + "- Gift";
        }

        if (NumberOfFormalEvents > 0)
        {
            PackingText += Environment.NewLine + $"- {NumberOfFormalEvents} Formal outfit(s)";
        }
    }
}
