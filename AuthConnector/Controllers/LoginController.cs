//https://github.com/Azure-Samples/active-directory-dotnet-external-identities-api-connector-azure-function-validate/blob/master/SignUpValidation.cs
using AuthConnector.Models;
using AuthConnector.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace AuthConnector.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly DbService _context;
        private readonly ILogger<LoginController> _logger;
        private readonly IConfiguration _configuration;

        public LoginController(DbService context, ILogger<LoginController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet(Name = "GetLogin")]
        public async Task<IActionResult> Get()
        {
            try
            {
                // Get the request body
                string requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

                // Get the json body using System.Text.Json
                dynamic data = JsonConvert.DeserializeObject(requestBody);


                // If input data is null, show block page
                //if (data == null)
                //{
                //    return (ActionResult)new OkObjectResult(new ResponseContent("ShowBlockPage", "There was a problem with your request."));
                //}

                // Check HTTP basic authorization using the Request property
                if (!Authorize())
                {
                    _logger.LogWarning("HTTP basic authentication validation failed.");
                    //return new UnauthorizedResult();
                }

                _logger.LogInformation("Login requested");

                var log = new Models.LogModel
                {
                    LogLevel = "Info",
                    Message = data.ToString()
                };

                await _context.Logs.AddAsync(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while logging the login request");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }

            //return Ok("Hello Login!");

            // Input validation passed successfully, return `Allow` response.
            // TO DO: Configure the claims you want to return
            return (ActionResult)new OkObjectResult(new ResponseContent()
            {
                //jobTitle = "This value return by the API Connector"//,
                // You can also return custom claims using extension properties.
                //extension_CustomClaim = "my custom claim response"
            });

        }

        private bool Authorize()
        {
            // Get user name and password from the appsettings using IConfiguration
            var username = _configuration["Auth:User"];
            var password = _configuration["Auth:Pass"];

            // Returns authorized if the username is empty or not exists.
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogInformation("HTTP basic authentication is not set.");
                return true;
            }

            // Check if the HTTP Authorization header exist
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                _logger.LogWarning("Missing HTTP basic authentication header.");
                return false;
            }

            // Read the authorization header
            var auth = Request.Headers["Authorization"].ToString();

            // Ensure the type of the authorization header id `Basic`
            if (!auth.StartsWith("Basic "))
            {
                _logger.LogWarning("HTTP basic authentication header must start with 'Basic '.");
                return false;
            }

            // Get the the HTTP basinc authorization credentials
            var cred = System.Text.UTF8Encoding.UTF8.GetString(Convert.FromBase64String(auth.Substring(6))).Split(':');

            // Log the credentials
            _logger.LogInformation($"HTTP basic authentication credentials: {cred[0]}:{cred[1]}");

            // Evaluate the credentials and return the result
            return (cred[0] == username && cred[1] == password);
        }
    }

}
