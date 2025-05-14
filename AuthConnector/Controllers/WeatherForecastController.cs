using AuthConnector.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthConnector.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly DbService _context;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(DbService context, ILogger<WeatherForecastController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            try
            {
                _logger.LogInformation("Weather forecast requested");

                var log = new Models.LogModel
                {
                    LogLevel = "Info",
                    Message = "Weather forecast requested"
                };

                await _context.Logs.AddAsync(log);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while logging the weather forecast request");
            }

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
