using AuthConnector.Models;
using AuthConnector.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Net.Http;

namespace AuthConnector.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CiamController : ControllerBase
    {
        private readonly DbService _context;
        private readonly ILogger<CiamController> _logger;
        private readonly IConfiguration _configuration;

        public CiamController(DbService context, ILogger<CiamController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            // Get the request body for POST
            return await HandleLoginAsync(isPost: true);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // No request body for GET
            return await HandleLoginAsync(isPost: false);
        }

        // Shared logic for both GET and POST
        private async Task<IActionResult> HandleLoginAsync(bool isPost)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                string? requestBody = Request.ContentLength > 0 ? await new StreamReader(Request.Body).ReadToEndAsync() : null;

                var log = new LogModel();

                if (!string.IsNullOrEmpty(requestBody))
                {
                    // Get the json body using Newtonsoft.Json
                    object? data = null;

                    try
                    {
                        data = JsonConvert.DeserializeObject(requestBody);

                        if (data == null)
                        {
                            _logger.LogWarning("Deserialized data is null.");
                        }
                        else
                        {
                            log.Message = data?.ToString() ?? string.Empty;

                            // Log the request body
                            await _context.Logs.AddAsync(log);
                            await _context.SaveChangesAsync();



                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogWarning(jsonEx, "Invalid JSON in request body.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
            }

            return new OkObjectResult(null);
        }

        public static async Task EnrolEmail(GraphServiceClient graphClient, string email, string objectId)
        {
            var emailAuthMethodRequestBody = new EmailAuthenticationMethod
            {
                EmailAddress = email
            };
            var result = await graphClient.Users[objectId].Authentication.EmailMethods.PostAsync(emailAuthMethodRequestBody);
            //return new OkObjectResult(enrolResult);
        }

        public static async Task DoWithRetryAsync(TimeSpan sleepPeriod, int tryCount = 3, string objectId = "test", string email = "test", GraphServiceClient graphClient = null)
        {
            if (tryCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(tryCount));

            while (true)
            {
                try
                {
                    await EnrolEmail(graphClient, email, objectId);
                    return;
                }
                catch
                {
                    if (--tryCount == 0)
                        throw;
                    await Task.Delay(sleepPeriod);
                }
            }
        }

        public class B2CResponseModel
        {
            public string version { get; set; }
            public int status { get; set; }
            public string userMessage { get; set; }

            public B2CResponseModel(string message, HttpStatusCode status)
            {
                this.userMessage = message;
                this.status = (int)status;
                this.version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }
    }
}
