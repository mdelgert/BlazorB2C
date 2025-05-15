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

                if (!string.IsNullOrWhiteSpace(requestBody))
                {
                    // Log the request body
                    log.Message = requestBody;

                    await _context.Logs.AddAsync(log);
                    await _context.SaveChangesAsync();

                    // Deserialize into a strongly-typed model
                    var ciamRequest = JsonConvert.DeserializeObject<CiamRequestModel>(requestBody!);

                    if (ciamRequest.method == "auth")
                    {

                        var scopes = new[] { "User.ReadWrite.All" };

                        // Multi-tenant apps can use "common",
                        // single-tenant apps must use the tenant ID from the Azure portal
                        var tenantId = "common";

                        // Value from app registration
                        var clientId = _configuration["AzureAd:ClientId"];

                        // using Azure.Identity;
                        var options = new UsernamePasswordCredentialOptions
                        {
                            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                        };

                        // https://learn.microsoft.com/dotnet/api/azure.identity.usernamepasswordcredential
                        var userNamePasswordCredential = new UsernamePasswordCredential(
                            ciamRequest.email, ciamRequest.password, tenantId, clientId, options);

                        var graphClient = new GraphServiceClient(userNamePasswordCredential, scopes);

                        // Get the user and log the details
                        var user = await graphClient.Users[ciamRequest.objectId].GetAsync();
                        
                        // Log the user details
                        log.Message = JsonConvert.SerializeObject(user);

                    }

                    // Log the deserialized object
                    log.Message = JsonConvert.SerializeObject(ciamRequest);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
            }

            return new OkObjectResult(null);
        }

        // Model for incoming request body
        public class CiamRequestModel
        {
            public string? objectId { get; set; }
            public string? email { get; set; }
            public string? password { get; set; }
            public string? method { get; set; }
            public string? phoneNumber { get; set; }
            public string? displayName { get; set; }
            public string? givenName { get; set; }
            public string? surName { get; set; }
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
                this.version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
            }
        }
    }
}
