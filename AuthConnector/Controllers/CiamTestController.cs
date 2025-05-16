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
    public class CiamTestController : ControllerBase
    {
        private readonly DbService _context;
        private readonly ILogger<CiamTestController> _logger;
        private readonly IConfiguration _configuration;

        public CiamTestController(DbService context, ILogger<CiamTestController> logger, IConfiguration configuration)
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

                await _context.Logs.AddAsync(new LogModel { LogLevel = "Info", Message = requestBody });
                await _context.SaveChangesAsync();

                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string objectId = data.objectId;
                string email = data.email;
                string password = data.password;
                string method = data.method;

                string phoneNumber = data.phoneNumber;
                string displayName = data.displayName;
                string givenName = data.givenName;
                string surName = data.surName;

                //var tenantId = "your-tenant-id-guid";
                var tenantId = _configuration["AzureAd:TenantId"];
                var tenantName = _configuration["AzureAd:TenantName"];

                if (method == "auth")
                {
                    using (var httpClient = new HttpClient())
                    {
                        // Build the request URL
                        var requestUrl = $"https://{tenantName}.ciamlogin.com/{tenantId}/oauth2/token";

                        string auth_resource = "https://graph.microsoft.com"; // Replace with your specific resource URL
                        
                        //string auth_clientId = "your-clientId-RopcFromB2C";
                        string auth_clientId = _configuration["AzureAd:ClientId"];

                        // Prepare the request body
                        var auth_requestBody = $"resource={auth_resource}&client_id={auth_clientId}&grant_type=password&username={email}&password={password}&nca=1";

                        // Convert the request body to a byte array
                        var content = new StringContent(auth_requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

                        // Send the POST request to Azure AD
                        using (var response = await httpClient.PostAsync(requestUrl, content))
                        {
                            // Check if the request was successful
                            if (response.IsSuccessStatusCode)
                            {
                                // Read the response content
                                //var responseContent = await response.Content.ReadAsStringAsync();
                                //var jsonObj = JsonConvert.DeserializeObject(responseContent);
                                //return new OkObjectResult(jsonObj);

                                var responseContent = await response.Content.ReadAsStringAsync();
                                return Content(responseContent, "application/json");
                            }
                            else
                            {
                                // Handle error cases here
                                // For example, _logger the error or throw an exception
                                return new ConflictObjectResult(new B2CResponseModel($"Invalid username or password.", HttpStatusCode.Conflict));
                            }
                        }
                    }
                }
                else
                {
                    var scopes = new[] { "https://graph.microsoft.com/.default" };
                    // Values from app registration  
                    
                    //var clientId = "your-client-id-to-call-graph";
                    var clientId = _configuration["AzureAd:ClientId"];

                    //var clientSecret = "your-client-secret";
                    var clientSecret = _configuration.GetSection("AzureAd:ClientCredentials")
                        .GetChildren()
                        .FirstOrDefault(c => c.GetValue<string>("SourceType") == "ClientSecret")
                        ?.GetValue<string>("ClientSecret");

                    // using Azure.Identity;  
                    var options = new TokenCredentialOptions
                    {
                        AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                    };

                    // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential  
                    var clientSecretCredential = new ClientSecretCredential(
                        tenantId, clientId, clientSecret, options);

                    // get accessToken          
                    var accessToken = await clientSecretCredential.GetTokenAsync(new Azure.Core.TokenRequestContext(scopes) { });

                    Console.WriteLine(accessToken.Token);

                    var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

                    if (objectId != null && method == "read")
                    {
                        var user = await graphClient.Users[objectId].GetAsync();
                        _logger.LogInformation(user.ToString());

                        var dto = new UserClaimsDto
                        {
                            ObjectId = user.Id,
                            DisplayName = user.DisplayName,
                            Mail = user.Mail,
                            Email = user.Mail,
                            //OtherMails = user.OtherMails != null ? user.OtherMails.ToList() : new List<string>(),
                            OtherMails = new List<string> { user.Mail },
                            GivenName = user.GivenName,
                            Surname = user.Surname,
                        };

                        return new OkObjectResult(dto);
                    }

                    //if (email != null && method == "read")
                    //{
                    //    var user = await graphClient.Users.GetAsync((requestConfiguration) =>
                    //    {
                    //        requestConfiguration.QueryParameters.Filter = string.Format("identities/any(x:x/issuerAssignedId eq '{0}' and x/issuer eq 'ciamprod.onmicrosoft.com')  ", email);
                    //    });
                    //    return new OkObjectResult(user);
                    //}

                    if (method == "createUser")
                    {
                        var userRequestBody = new User
                        {
                            DisplayName = displayName,
                            Identities = new List<ObjectIdentity>
                    {
                        new ObjectIdentity
                        {
                            SignInType = "emailAddress",
                            Issuer = "ciamprod.onmicrosoft.com",
                            IssuerAssignedId = email,
                        }
                    },
                            PasswordProfile = new PasswordProfile
                            {
                                Password = password,
                                ForceChangePasswordNextSignIn = false,
                            },
                            PasswordPolicies = "DisablePasswordExpiration",
                        };
                        try
                        {
                            var result = await graphClient.Users.PostAsync(userRequestBody);
                            string stringObjectId = result.Id;

                            try
                            {
                                await DoWithRetryAsync(TimeSpan.FromSeconds(1), tryCount: 10, stringObjectId, email, graphClient);

                            }
                            catch (Exception enrolEx)
                            {
                                return new ConflictObjectResult(enrolEx);
                            }

                            return new OkObjectResult(result);
                        }
                        catch (Exception ex)
                        {
                            return new ConflictObjectResult(new B2CResponseModel($"This account already exists.", HttpStatusCode.Conflict));
                        }
                    }

                    if (method == "getPhone")
                    {
                        try
                        {
                            var result = await graphClient.Users[objectId].Authentication.PhoneMethods.GetAsync();
                            return new OkObjectResult(result.Value[0]);
                        }

                        catch (Exception exception)
                        {
                            var jsonObject = new JObject();
                            //jsonObject.Add("phoneNumber", "null");
                            jsonObject.Add("phoneNumberString", "null");
                            return new OkObjectResult(jsonObject);
                        }

                    }

                    if (method == "setPhone")
                    {

                        var mfaRequestBody = new PhoneAuthenticationMethod
                        {
                            PhoneNumber = phoneNumber,
                            PhoneType = AuthenticationPhoneType.Mobile,
                        };
                        var enrolResult = await graphClient.Users[objectId].Authentication.PhoneMethods.PostAsync(mfaRequestBody);
                        return new OkObjectResult(enrolResult);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                //throw;
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

        // DTO for returning user claims
        public class UserClaimsDto
        {
            public string? ObjectId { get; set; }
            public string? DisplayName { get; set; }
            public string? Mail { get; set; }
            public string? Email { get; set; }
            public List<string>? OtherMails { get; set; }
            public string? GivenName { get; set; }
            public string? Surname { get; set; }
        }
    }
}
