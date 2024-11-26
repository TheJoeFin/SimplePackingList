using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Windows.ApplicationModel.DataTransfer;

namespace SimplePackingList.ViewModels;
public partial class MainViewModel : ObservableObject
{
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

    public MainViewModel()
    {
        
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

    private void UpdateDaysAndText()
    {
        if (EndDate is null || StartDate is null)
            return;

        numberOfDays = (int)(EndDate.Value - StartDate.Value).TotalDays;
        numberOfDays = Math.Abs(numberOfDays);
        PackingText = $"Packing List for {StartDate:MMM dd} - {EndDate:MMM dd} for {numberOfDays} nights";

        PackingText += Environment.NewLine + standardStuff;

        string clothes = $"""

            - {numberOfDays} shirts
            - { Math.Ceiling( numberOfDays / 3.0)} pair(s) of pants
            - {numberOfDays} pair(s) of socks
            - {numberOfDays} pair(s) of underwear
            """;

        PackingText += Environment.NewLine + clothes;

        if (IsRunning)
        {
            int numberOfRuns = (int)Math.Ceiling(numberOfDays / 3.0);
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
