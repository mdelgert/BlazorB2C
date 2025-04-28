using Microsoft.AspNetCore.Mvc;

//https://localhost:7155/EnvironmentSecure

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EnvironmentSecureController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EnvironmentController> _logger;

        public EnvironmentSecureController(IWebHostEnvironment environment, ILogger<EnvironmentController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        [HttpGet(Name = "GetEnvironmentSecure")]
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