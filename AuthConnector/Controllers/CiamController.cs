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
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

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
                        var clientId = _configuration["AzureAd:ClientId"];
                        var tenant = _configuration["AzureAd:TenantId"];
                        var b2cDomain = _configuration["AzureAd:Domain"]; // e.g. yourtenant.b2clogin.com
                        var policy = _configuration["AzureAd:SignInPolicy"]; // e.g. B2C_1A_ROPC_Auth

                        //https://learn.microsoft.com/en-us/azure/active-directory-b2c/tokens-overview#endpoints
                        //Not working, not sure why ciamlogin.com does work have not found documentation. $"https://{tenantName}.ciamlogin.com/{tenantId}/oauth2/token";
                        //var authority = $"https://{b2cDomain}/{tenant}/{policy}/v2.0/";
                        //var authority = $"https://{b2cDomain}/{tenant}/{policy}/oauth2/v2.0/authorize";
                        var authority = $"https://{b2cDomain}/{tenant}/{policy}/oauth2/v2.0/token";

                        //Log the authority URL
                        _logger.LogInformation($"Authority URL: {authority}");

                        var app = PublicClientApplicationBuilder.Create(clientId)
                            .WithB2CAuthority(authority)
                            .Build();

                        var scopes = new[] { "https://graph.microsoft.com/User.Read" };

                        try
                        {
                            //TODO replaced obsolete AcquireTokenByUsernamePassword with AcquireTokenByUsernamePassword
                            var result = await app.AcquireTokenByUsernamePassword(
                                scopes,
                                ciamRequest.email,
                                new System.Net.NetworkCredential("", ciamRequest.password).SecurePassword
                            ).ExecuteAsync();

                            // Use the access token to call Microsoft Graph (Graph SDK v5+)
                            var httpClient = new HttpClient();
                            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.AccessToken);
                            var graphClient = new GraphServiceClient(httpClient);

                            var user = await graphClient.Users[ciamRequest.objectId].GetAsync();
                            return new OkObjectResult(user);
                        }
                        catch (MsalUiRequiredException ex)
                        {
                            return BadRequest($"Authentication failed: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            return StatusCode(500, $"Error: {ex.Message}");
                        }
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