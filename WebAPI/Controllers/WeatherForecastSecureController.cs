using Shared.Models;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ServiceFilter(typeof(AuthService))]
    public class WeatherForecastSecureController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastSecureController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecastSecure")]
        public IEnumerable<WeatherForecast> Get()
        {
            _logger.LogInformation("Getting weather forecast");

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = WeatherData.Summaries[Random.Shared.Next(WeatherData.Summaries.Length)]
            })
            .ToArray();
        }
    }
}
