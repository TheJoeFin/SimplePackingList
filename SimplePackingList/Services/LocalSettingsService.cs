using System;
using System.Diagnostics;
using System.Text.Json;
using Windows.Storage;

namespace SimplePackingList.Services;

public static class LocalSettingsService
{
    static public T? ReadSettingAsync<T>(string key)
    {
        try
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out object? obj))
            {
                return JsonSerializer.Deserialize<T>((string)obj);
            }
        }
        catch (Exception)
        {
            Debug.WriteLine($"Failed to read setting: {key}");
        }
        return default;

    }

    static public void SaveSettingAsync<T>(string key, T value)
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values[key] = JsonSerializer.Serialize(value);
        }
        catch (Exception)
        {
            Debug.WriteLine($"Failed to write setting: {key}\t{value?.ToString()}");
        }
    }
}

public enum SettingKeys
{
    LastTrip,
    PastTrips,
}