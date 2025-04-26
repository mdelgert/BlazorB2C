using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EnvironmentController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EnvironmentController> _logger;

        public EnvironmentController(IWebHostEnvironment environment, ILogger<EnvironmentController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        [HttpGet(Name = "GetEnvironment")]
        public IActionResult Get()
        {
            _logger.LogInformation("Getting environment information");

            var environmentInfo = new
            {
                EnvironmentName = _environment.EnvironmentName,
                ApplicationName = _environment.ApplicationName,
                ContentRootPath = _environment.ContentRootPath,
                WebRootPath = _environment.WebRootPath,
                IsDevelopment = _environment.IsDevelopment(),
                IsProduction = _environment.IsProduction(),
                IsStaging = _environment.IsStaging()
            };

            return Ok(environmentInfo);
        }
    }
}