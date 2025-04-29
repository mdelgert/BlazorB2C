using Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Identity.Web;
using Microsoft.Identity.Abstractions;

public class WeatherForecastBase : ComponentBase
{
    [Inject] 
    ILogger<Index> Logger { get; set; } = default!;

    [Inject]
    IDownstreamApi DownstreamApi { get; set; }

    [Inject]
    NavigationManager Navigation { get; set; }

    const string ServiceName = "DownstreamApi";

    //protected override async Task OnInitializedAsync()
    //{
    //    await GetWeatherForecast();
    //}

    /// <summary>
    /// Gets weather forecast data from the downstream API.
    /// </summary>
    /// <returns></returns>
    public async Task GetWeatherForecast()
    {
        try
        {
            var weather = (await DownstreamApi.GetForUserAsync<IEnumerable<WeatherForecast>>(
                    ServiceName,
                    options => options.RelativePath = "/WeatherForecast"))!;

            // Log the weather forecast data
            foreach (var forecast in weather)
            {
                Logger.LogInformation($"Date: {forecast.Date}, TemperatureC: {forecast.TemperatureC}, Summary: {forecast.Summary}");
            }

            Logger.LogInformation("Getting weather forecast data from the downstream API...");
        }
        catch (Exception ex)
        {
            // Log the exception
            Logger.LogError(ex, "An error occurred while getting weather forecast data.");
        }
    }
}
