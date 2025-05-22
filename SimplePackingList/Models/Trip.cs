using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;

namespace SimplePackingList.Models;

public partial class Trip : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PlaceEntry { get; set; } = string.Empty;

    [ObservableProperty]
    public partial PlacePrediction? Place { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? StartDate { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? EndDate { get; set; }

    [ObservableProperty]
    public partial OpenWeatherResponse? OpenWeatherResponse { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset GotForecastOn { get; set; }

    [ObservableProperty]
    public partial string PackingText { get; set; } = string.Empty;

    public Trip()
    {
        
    }

    public static Trip NewTrip()
    {
        Trip trip = new()
        {
            Id = Guid.NewGuid().ToString()
        };
        return trip;
    }
}
