namespace Shared.Models;

/// <summary>
/// Contains shared weather data used across API projects
/// </summary>
public static class WeatherData
{
    /// <summary>
    /// Collection of weather summary descriptions
    /// </summary>
    public static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };
}