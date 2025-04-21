using SimplePackingList.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimplePackingList.Services;

public interface IWeatherService
{
    Task<WeatherInfo?> GetWeatherForecastAsync(double latitude, double longitude, DateTimeOffset date, CancellationToken cancellationToken);
}
