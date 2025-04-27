To call a secured Azure AD B2C-protected API from your Blazor Server front-end, you need to:

1. Ensure the API is correctly configured with Azure AD B2C authentication and exposes the necessary scopes.
2. Configure the Blazor Server front-end to acquire an access token for the API using the authenticated user's identity.
3. Use the access token to make authorized HTTP requests to the API.

Below is a step-by-step guide to achieve this, building on your provided API code and assuming your Blazor Server app is already configured with Azure AD B2C authentication (as per our previous discussion). I'll explain how to set up the API, configure the front-end, and call the API securely.

---

### Prerequisites
- **Blazor Server App**: Already configured with Azure AD B2C authentication (using `Microsoft.Identity.Web` and `CascadingAuthenticationState` as described earlier).
- **API Project**: A .NET 8 Web API project with Azure AD B2C authentication, as shown in your `WeatherForecastController`.
- **Azure AD B2C Tenant**: Both the Blazor app and API are registered in the same B2C tenant.
- **Tools**: Visual Studio 2022, .NET 8 SDK, or CLI.

---

### Step 1: Configure the API for Azure AD B2C
Your `WeatherForecastController` is already decorated with `[Authorize]` and `[RequiredScope]`, which is a good start. Below, I'll ensure the API is fully configured to validate B2C access tokens and expose scopes.

#### 1.1. **Register the API in Azure AD B2C**
1. **Create an App Registration for the API**:
   - In the Azure portal, navigate to your Azure AD B2C tenant > **App registrations** > **New registration**.
   - Name: `WeatherAPI`.
   - **Supported account types**: Select **Accounts in any identity provider or organizational directory (for authenticating users with user flows)**.
   - **Redirect URI**: Leave blank (APIs don't need redirect URIs).
   - Click **Register**.
   - Note the **Application (client) ID** (e.g., `22223333-cccc-4444-dddd-5555eeee6666`).

2. **Expose an API**:
   - In the API's app registration, go to **Expose an API**.
   - Click **Set** next to **Application ID URI** and set it to `api://22223333-cccc-4444-dddd-5555eeee6666` (or a custom URI like `https://yourtenant.onmicrosoft.com/weatherapi`).
   - Click **Add a scope**.
   - Scope name: `access_as_user` (or another name, e.g., `Weather.Read`).
   - Admin consent display name: `Access Weather API`.
   - Admin consent description: `Allows access to the Weather API`.
   - Save the scope and note the full scope URI (e.g., `api://22223333-cccc-4444-dddd-5555eeee6666/access_as_user`).

3. **Grant Permissions to the Blazor App**:
   - Go to the app registration for your Blazor Server app (e.g., `BlazorB2CServerApp` with Client ID `11112222-bbbb-3333-cccc-4444dddd5555`).
   - Navigate to **API permissions** > **Add a permission** > **My APIs**.
   - Select the `WeatherAPI` app and check the `access_as_user` scope.
   - Click **Add permissions**.
   - Click **Grant admin consent for <Your Tenant>** to approve the permission.

#### 1.2. **Update API Configuration**
Ensure your API project is configured to validate Azure AD B2C tokens and enforce the scope.

1. **Update `appsettings.json`**:
   Add Azure AD B2C settings and the required scope:
   ```json
   {
     "AzureAd": {
       "Instance": "https://yourtenant.b2clogin.com/",
       "Domain": "yourtenant.onmicrosoft.com",
       "TenantId": "your-tenant-id",
       "ClientId": "22223333-cccc-4444-dddd-5555eeee6666",
       "SignUpSignInPolicyId": "B2C_1_signupsignin",
       "Scopes": "https://yourtenant.onmicrosoft.com/22223333-cccc-4444-dddd-5555eeee6666/access_as_user"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft": "Warning",
         "Microsoft.Hosting.Lifetime": "Information"
       }
     },
     "AllowedHosts": "*"
   }
   ```
   - Replace placeholders with your B2C tenant details.
   - `ClientId` is the API's app registration ID.
   - **Important**: The `Scopes` value MUST match exactly how the scope is defined in Azure AD B2C. Note the full URI format that includes your tenant name and application ID.

2. **Update `Program.cs`**:
   Configure JWT bearer authentication for Azure AD B2C:
   ```csharp
   using Microsoft.AspNetCore.Authentication.JwtBearer;
   using Microsoft.Identity.Web;

   var builder = WebApplication.CreateBuilder(args);

   // Add services to the container.
   builder.Services.AddControllers();

   // Configure Azure AD B2C authentication
   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

   // Add authorization
   builder.Services.AddAuthorization();

   var app = builder.Build();

   // Configure the HTTP request pipeline.
   if (app.Environment.IsDevelopment())
   {
       app.UseDeveloperExceptionPage();
   }
   else
   {
       app.UseExceptionHandler("/Error");
       app.UseHsts();
   }

   app.UseHttpsRedirection();
   app.UseRouting();

   app.UseAuthentication();
   app.UseAuthorization();

   app.MapControllers();

   app.Run();
   ```
   - `AddMicrosoftIdentityWebApi` configures the API to validate B2C-issued JWT tokens.
   - The `[RequiredScope("access_as_user")]` attribute in your controller ensures the token includes the specified scope.

3. **Verify `WeatherForecastController`**:
   Your controller is correct, but for clarity, here it is with comments:
   ```csharp
   using Microsoft.AspNetCore.Authorization;
   using Microsoft.AspNetCore.Mvc;
   using Microsoft.Identity.Web.Resource;

   namespace WebAPI.Controllers
   {
       [Authorize]
       [ApiController]
       [Route("[controller]")]
       [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
       public class WeatherForecastController : ControllerBase
       {
           // Controller implementation...
       }
   }
   ```
   - The `[Authorize]` attribute requires a valid token.
   - `[RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]` ensures the token includes the scope defined in your configuration.

---

### Step 2: Configure the Blazor Server Front-End
The Blazor Server app needs to acquire an access token for the API and include it in HTTP requests. We'll use `Microsoft.Identity.Web` to handle token acquisition.

#### 2.1. **Update `appsettings.json`**
Add the API scope to the Blazor app's configuration to request it during authentication:
```json
{
  "AzureAdB2C": {
    "Instance": "https://yourtenant.b2clogin.com/tfp/",
    "ClientId": "11112222-bbbb-3333-cccc-4444dddd5555",
    "Domain": "yourtenant.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_signupsignin",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "ClientSecret": "your-client-secret",
    "Scopes": ["https://yourtenant.onmicrosoft.com/22223333-cccc-4444-dddd-5555eeee6666/access_as_user"]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",
  "WeatherApi": {
    "BaseUrl": "https://localhost:<api-port>" // e.g., https://localhost:7252
  }
}
```
- **IMPORTANT**: The `Scopes` array must use the exact format expected by your B2C tenant. For B2C, this is typically in the format: `https://{tenant-name}.onmicrosoft.com/{app-id}/access_as_user`. Using an incorrect scope format will result in an "AADB2C90117: The scope provided in the request is not supported" error.
- **WeatherApi:BaseUrl**: The API's base URL (update with the actual port or production URL).

#### 2.2. **Configure `Program.cs`**
Register token acquisition services in `Program.cs`:
```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Azure AD B2C authentication with token acquisition
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options => {
        builder.Configuration.GetSection("AzureAdB2C").Bind(options);
        options.ResponseType = "code";
    })
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

// Register HttpContextAccessor - needed for token acquisition
builder.Services.AddHttpContextAccessor();

// Add HttpClient for calling the API
builder.Services.AddHttpClient();

// Add authorization
builder.Services.AddAuthorization();

// Add Microsoft Identity UI for login/logout
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Add RazorPages needed for authentication
builder.Services.AddRazorPages();

// ...rest of your Program.cs setup
```
- **EnableTokenAcquisitionToCallDownstreamApi**: Configures the app to acquire access tokens for the API scope.
- **AddInMemoryTokenCaches**: Caches tokens to avoid repeated requests to B2C.
- **AddHttpContextAccessor**: Required for accessing user claims during token acquisition.

#### 2.3. **Create a Service to Call the API**
Create a service in the Blazor app to handle API calls with the access token:

```csharp
using Microsoft.Identity.Web;
using System.Net.Http.Headers;
using Shared.Models;

namespace BlazorApp.Services
{
    public class SecureApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecureApiService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SecureApiService(
            IHttpClientFactory httpClientFactory,
            ITokenAcquisition tokenAcquisition,
            IConfiguration configuration,
            ILogger<SecureApiService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _tokenAcquisition = tokenAcquisition;
            _configuration = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<WeatherForecast[]?> GetWeatherForecastsAsync()
        {
            try
            {
                // Get API endpoint from configuration
                var apiEndpoint = _configuration["WeatherApi:SecureUrl"];
                if (string.IsNullOrEmpty(apiEndpoint))
                {
                    _logger.LogError("SecureAPI endpoint is not configured in appsettings.json");
                    return null;
                }

                // Get the scopes from configuration
                var scopes = _configuration.GetSection("AzureAdB2C:Scopes").Get<string[]>();
                if (scopes == null || scopes.Length == 0)
                {
                    _logger.LogError("API scopes are not configured in appsettings.json");
                    return null;
                }
                
                _logger.LogInformation("Acquiring access token for scopes: {Scopes}", string.Join(", ", scopes));
                
                // Get the user from HttpContext
                var user = _httpContextAccessor.HttpContext?.User;
                if (user == null || !user.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("User is not authenticated or HttpContext is not available");
                    throw new UnauthorizedAccessException("User is not authenticated");
                }

                // Acquire the token
                var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);

                // Create an HTTP client
                using var client = _httpClientFactory.CreateClient();
                
                // Set the Authorization header with the access token
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                _logger.LogInformation("Calling secure API at: {ApiEndpoint}/WeatherForecast", apiEndpoint);

                // Call the API with the token
                var response = await client.GetAsync($"{apiEndpoint}/WeatherForecast");
                response.EnsureSuccessStatusCode();

                // Parse the response
                var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
                
                _logger.LogInformation("Successfully retrieved weather data from secure API");
                
                return forecasts;
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
            {
                // This exception is thrown when user authentication is required
                _logger.LogWarning("Authentication challenge required: {Message}", ex.Message);
                throw; // Let the framework handle the authentication challenge
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling secure API: {Message}", ex.Message);
                return null;
            }
        }
    }
}
```

#### 2.4. **Create a Blazor Component to Call the API**
Create a component that uses the service to display API data:

```razor
@page "/weathersecureapi"
@rendermode InteractiveServer
@attribute [Authorize]
@inject BlazorApp.Services.SecureApiService SecureApiService
@inject IConfiguration Configuration

<PageTitle>Weather Secure API</PageTitle>

<h1>Weather Secure API</h1>

<p>This component demonstrates fetching data from a secured API endpoint.</p>

<AuthorizeView>
    <Authorized>
        @if (forecasts == null)
        {
            <p><em>Loading...</em></p>
        }
        else if (forecasts.Length == 0)
        {
            <p>No weather data available.</p>
        }
        else
        {
            <table class="table">
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Temp. (C)</th>
                        <th>Temp. (F)</th>
                        <th>Summary</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var forecast in forecasts)
                    {
                        <tr>
                            <td>@forecast.Date.ToShortDateString()</td>
                            <td>@forecast.TemperatureC</td>
                            <td>@forecast.TemperatureF</td>
                            <td>@forecast.Summary</td>
                        </tr>
                    }
                </tbody>
            </table>
        }
    </Authorized>
    <NotAuthorized>
        <p>You must be logged in to view weather data.</p>
        <a href="MicrosoftIdentity/Account/SignIn">Click here to log in</a>
    </NotAuthorized>
</AuthorizeView>

@code {
    private WeatherForecast[]? forecasts;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            forecasts = await SecureApiService.GetWeatherForecastsAsync();
        }
        catch (Exception ex)
        {
            // Handle or log the exception
        }
    }
}
```

---

### Common Issues and Troubleshooting

#### Scope Format Issues
The most common issue when integrating Azure B2C with a secure API is the scope format. If you see an error like:

```
AADB2C90117: The scope 'api://64417a7e-e597-44a5-9cd9-b5e16f78116d/access_as_user' provided in the request is not supported.
```

It means the scope format you're using doesn't match what's configured in your B2C tenant. Try these formats, in order:

1. **Standard B2C Format**: `https://{your-tenant}.onmicrosoft.com/{api-app-id}/access_as_user`
2. **API URI Format**: `api://{api-app-id}/access_as_user`
3. **Simple Format**: `access_as_user` (if configured this way in your B2C tenant)

Make sure the scope format in the Blazor app's configuration matches exactly how it's defined in the Azure B2C tenant.

#### Authentication Flow Issues in Blazor Server
For Blazor Server components, ensure that:

1. Add `@rendermode InteractiveServer` to your component to enable interactivity
2. Add `@attribute [Authorize]` to require authentication
3. Register `IHttpContextAccessor` in `Program.cs`
4. Use the `AuthenticationStateProvider` to get the current user in components

#### Token Acquisition Failures
If you get "No account or login hint was passed to the AcquireTokenSilent call" errors:

1. Ensure the user is properly authenticated before attempting to get a token
2. Make sure you're using `IHttpContextAccessor` to access the current user's claims
3. Call `StateHasChanged()` after async operations in Blazor components
4. Register token acquisition services correctly in `Program.cs`

---

### Logging and Debugging Tips

To debug authentication issues:

1. **Enable Detailed Logging**: 
   ```json
   "Logging": {
     "LogLevel": {
       "Default": "Information",
       "Microsoft": "Warning",
       "Microsoft.Identity": "Debug",
       "Microsoft.AspNetCore.Authentication": "Debug"
     }
   }
   ```

2. **Check Token Claims**: Decode your access token at [jwt.ms](https://jwt.ms/) to verify scopes

3. **Add Diagnostic Code**: Temporarily add this to your service:
   ```csharp
   var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
   _logger.LogInformation("Token acquired: {TokenStart}...", 
       accessToken.Substring(0, Math.Min(10, accessToken.Length)));
   ```

4. **Check Authentication State**: Use `ILogger` to log the user's authentication state:
   ```csharp
   _logger.LogInformation("User authenticated: {IsAuthenticated}, Name: {Name}", 
       user.Identity?.IsAuthenticated, user.Identity?.Name ?? "Unknown");
   ```

### Additional Resources
- [Microsoft Documentation: Call a web API from Blazor Server](https://learn.microsoft.com/en-us/azure/active-directory-b2c/secure-web-api?tabs=visual-studio)
- [Microsoft Identity Web Sample](https://github.com/Azure-Samples/ms-identity-blazor-server)
- [Troubleshooting Azure AD B2C](https://learn.microsoft.com/en-us/azure/active-directory-b2c/troubleshoot?pivots=b2c-user-flow)