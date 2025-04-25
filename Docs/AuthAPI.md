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
   - **Redirect URI**: Leave blank (APIs don’t need redirect URIs).
   - Click **Register**.
   - Note the **Application (client) ID** (e.g., `22223333-cccc-4444-dddd-5555eeee6666`).

2. **Expose an API**:
   - In the API’s app registration, go to **Expose an API**.
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
       "Instance": "https://yourtenant.b2clogin.com",
       "Domain": "yourtenant.onmicrosoft.com",
       "TenantId": "your-tenant-id",
       "ClientId": "22223333-cccc-4444-dddd-5555eeee6666",
       "SignUpSignInPolicyId": "B2C_1_signupsignin"
     },
     "AzureAd:Scopes": "access_as_user",
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
   - `ClientId` is the API’s app registration ID.
   - `Scopes` matches the scope defined in Azure AD B2C (`access_as_user`).

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
           private static readonly string[] Summaries = new[]
           {
               "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
           };

           private readonly ILogger<WeatherForecastController> _logger;

           public WeatherForecastController(ILogger<WeatherForecastController> logger)
           {
               _logger = logger;
           }

           [HttpGet(Name = "GetWeatherForecast")]
           public IEnumerable<WeatherForecast> Get()
           {
               return Enumerable.Range(1, 5).Select(index => new WeatherForecast
               {
                   Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                   TemperatureC = Random.Shared.Next(-20, 55),
                   Summary = Summaries[Random.Shared.Next(Summaries.Length)]
               })
               .ToArray();
           }
       }

       public class WeatherForecast
       {
           public DateOnly Date { get; set; }
           public int TemperatureC { get; set; }
           public string? Summary { get; set; }
       }
   }
   ```
   - The `[Authorize]` attribute requires a valid token.
   - `[RequiredScope("AzureAd:Scopes")]` ensures the token includes the `access_as_user` scope.

4. **Test the API**:
   - Run the API project (`dotnet run`).
   - Use a tool like Postman to test the `/WeatherForecast` endpoint:
     - **URL**: `https://localhost:<port>/WeatherForecast`.
     - **Authorization**: Bearer token (obtained manually from B2C or via the Blazor app later).
     - Without a valid token or scope, you should get a `401 Unauthorized` or `403 Forbidden` response.

---

### Step 2: Configure the Blazor Server Front-End
The Blazor Server app needs to acquire an access token for the API and include it in HTTP requests. We’ll use `Microsoft.Identity.Web` to handle token acquisition.

#### 2.1. **Update `appsettings.json`**
Add the API scope to the Blazor app’s configuration to request it during authentication:
```json
{
  "AzureAdB2C": {
    "Instance": "https://yourtenant.b2clogin.com",
    "ClientId": "11112222-bbbb-3333-cccc-4444dddd5555",
    "Domain": "yourtenant.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_signupsignin",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "ClientSecret": "your-client-secret",
    "Scope": [ "openid", "offline_access", "api://22223333-cccc-4444-dddd-5555eeee6666/access_as_user" ]
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
    "BaseUrl": "https://localhost:<api-port>" // e.g., https://localhost:7001
  }
}
```
- **Scope**: Includes the API scope (`api://22223333-cccc-4444-dddd-5555eeee6666/access_as_user`) to request it in the access token.
- **WeatherApi:BaseUrl**: The API’s base URL (update with the actual port or production URL).

#### 2.2. **Add Token Acquisition Service**
Register the token acquisition service in `Program.cs` to enable the Blazor app to acquire access tokens:
```csharp
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Azure AD B2C authentication with token acquisition
builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAdB2C")
    .EnableTokenAcquisitionToCallDownstreamApi(new[] { "api://22223333-cccc-4444-dddd-5555eeee6666/access_as_user" })
    .AddInMemoryTokenCaches();

// Add authorization
builder.Services.AddAuthorization();

// Add HttpClient for calling the API
builder.Services.AddHttpClient("WeatherApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WeatherApi:BaseUrl"]!);
});

// Add Microsoft Identity UI for login/logout
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.Run();
```
- **EnableTokenAcquisitionToCallDownstreamApi**: Configures the app to acquire access tokens for the API scope.
- **AddInMemoryTokenCaches**: Caches tokens to avoid repeated requests to B2C.
- **AddHttpClient**: Registers an `HttpClient` for calling the API.

#### 2.3. **Create a Service to Call the API**
Create a service in the Blazor app to handle API calls with the access token.

1. **Add a Model**:
   In the Blazor project, create a `Models` folder and add `WeatherForecast.cs` (to match the API’s response):
   ```csharp
   namespace BlazorB2CServerApp.Models;

   public class WeatherForecast
   {
       public DateOnly Date { get; set; }
       public int TemperatureC { get; set; }
       public string? Summary { get; set; }
   }
   ```

2. **Create a Weather Service**:
   In the Blazor project, create a `Services` folder and add `WeatherService.cs`:
   ```csharp
   using BlazorB2CServerApp.Models;
   using Microsoft.Identity.Web;

   namespace BlazorB2CServerApp.Services;

   public class WeatherService
   {
       private readonly IHttpClientFactory _httpClientFactory;
       private readonly ITokenAcquisition _tokenAcquisition;

       public WeatherService(IHttpClientFactory httpClientFactory, ITokenAcquisition tokenAcquisition)
       {
           _httpClientFactory = httpClientFactory;
           _tokenAcquisition = tokenAcquisition;
       }

       public async Task<WeatherForecast[]?> GetWeatherForecastsAsync()
       {
           try
           {
               // Acquire an access token for the API
               var scopes = new[] { "api://22223333-cccc-4444-dddd-5555eeee6666/access_as_user" };
               var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);

               // Create an HTTP client and set the Authorization header
               var client = _httpClientFactory.CreateClient("WeatherApi");
               client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

               // Call the API
               var response = await client.GetAsync("/WeatherForecast");
               response.EnsureSuccessStatusCode();

               return await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
           }
           catch (MicrosoftIdentityWebChallengeUserException)
           {
               // Handle cases where the user needs to re-authenticate
               throw;
           }
           catch (Exception ex)
           {
               // Log or handle other errors
               Console.WriteLine($"Error calling API: {ex.Message}");
               return null;
           }
       }
   }
   ```
   - **ITokenAcquisition**: Used to acquire the access token for the API scope.
   - **IHttpClientFactory**: Provides an `HttpClient` configured with the API’s base URL.
   - The service adds the access token to the `Authorization` header and calls the `/WeatherForecast` endpoint.

3. **Register the Service**:
   In `Program.cs`, add the service to the DI container (before `var app = builder.Build()`):
   ```csharp
   builder.Services.AddScoped<WeatherService>();
   ```

#### 2.4. **Update a Blazor Component to Call the API**
Modify a component (e.g., `Home.razor`) to call the API and display the results.

Update `Pages/Home.razor`:
```razor
@page "/"
@using BlazorB2CServerApp.Models
@using BlazorB2CServerApp.Services
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize]
@inject WeatherService WeatherService

<PageTitle>Home</PageTitle>

<h1>Weather Forecast</h1>

@if (forecasts == null)
{
    <p>Loading...</p>
}
else if (!forecasts.Any())
{
    <p>No forecasts available.</p>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Date</th>
                <th>Temp. (C)</th>
                <th>Summary</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var forecast in forecasts)
            {
                <tr>
                    <td>@forecast.Date.ToShortDateString()</td>
                    <td>@forecast.TemperatureC</td>
                    <td>@forecast.Summary</td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private WeatherForecast[]? forecasts;

    protected override async Task OnInitializedAsync()
    {
        forecasts = await WeatherService.GetWeatherForecastsAsync();
    }
}
```
- **@attribute [Authorize]**: Ensures only authenticated users can access the page.
- **WeatherService**: Injected to call the API.
- **OnInitializedAsync**: Calls the API when the component loads and stores the results in `forecasts`.

#### 2.5. **Update `_Imports.razor`**
Ensure `_Imports.razor` includes the necessary namespaces:
```razor
@using System.Net.Http
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using BlazorB2CServerApp
@using BlazorB2CServerApp.Models
@using BlazorB2CServerApp.Services
```

---

### Step 3: Test the Application
1. **Run Both Projects**:
   - Start the API project:
     ```bash
     cd WebAPI
     dotnet run
     ```
   - Start the Blazor Server project:
     ```bash
     cd BlazorB2CServerApp
     dotnet run
     ```
   - Note the ports (e.g., API on `https://localhost:7001`, Blazor on `https://localhost:5001`).

2. **Test the Flow**:
   - Open the Blazor app (e.g., `https://localhost:5001`).
   - Log in via Azure AD B2C (using the `AuthorizeView` in `MainLayout.razor`).
   - Navigate to the `/` (Home) page.
   - The page should display the weather forecasts fetched from the API.
   - If unauthenticated, you’ll be redirected to the B2C login page.

3. **Verify Token Usage**:
   - Check the API logs to confirm the `/WeatherForecast` endpoint is called with a valid Bearer token.
   - If you see `401 Unauthorized`, ensure the token includes the `access_as_user` scope (debug the token using a JWT decoder like jwt.ms).
   - If you see `403 Forbidden`, verify the scope in `appsettings.json` and the `[RequiredScope]` attribute.

---

### Troubleshooting
- **401 Unauthorized**:
  - Ensure the API’s `ClientId` and `TenantId` in `appsettings.json` match the API’s app registration.
  - Verify the Blazor app requests the correct scope (`api://22223333-cccc-4444-dddd-5555eeee6666/access_as_user`).
  - Check that the Blazor app has permission to the API scope in Azure AD B2C.

- **403 Forbidden**:
  - Confirm the `[RequiredScope]` attribute matches the scope in `appsettings.json` (`access_as_user`).
  - Ensure the access token includes the scope (use `ITokenAcquisition` to log the token for debugging).

- **Token Acquisition Fails**:
  - Verify `EnableTokenAcquisitionToCallDownstreamApi` is called in `Program.cs` with the correct scope.
  - Ensure the user is authenticated before calling the API (the `[Authorize]` attribute on `Home.razor` helps).

- **CORS Issues** (if API and Blazor are on different domains):
  - Add CORS to the API’s `Program.cs`:
    ```csharp
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowBlazor", builder =>
        {
            builder.WithOrigins("https://localhost:5001")
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
    });

    // In the pipeline, before UseRouting
    app.UseCors("AllowBlazor");
    ```
  - Update the origin for production (e.g., `https://yourblazorapp.azurewebsites.net`).

- **Null or Empty Forecasts**:
  - Check the `WeatherService` for exceptions (log errors in the `catch` block).
  - Ensure the API’s base URL in `appsettings.json` is correct.

---

### Additional Notes
- **Token Caching**: `AddInMemoryTokenCaches` is suitable for development. For production, consider `AddDistributedTokenCaches` with a distributed cache (e.g., Redis) for scalability.
- **Refresh Tokens**: `Microsoft.Identity.Web` handles token refresh automatically if `offline_access` is included in the scopes.
- **Production**:
  - Update `WeatherApi:BaseUrl` and CORS origins for production.
  - Secure the client secret using Azure Key Vault or environment variables.
  - Ensure both apps use HTTPS.
- **Debugging Tokens**:
  - To inspect the access token, temporarily modify `WeatherService`:
    ```csharp
    var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
    Console.WriteLine($"Access Token: {accessToken}");
    ```
  - Decode the token at jwt.ms to verify the `scp` (scope) claim includes `access_as_user`.

---

### Citations
-: Guidance on calling a protected API with Azure AD B2C.
-: Details on `Microsoft.Identity.Web` for token acquisition.
-: Community discussion on Blazor Server calling secured APIs.

If you encounter specific errors (e.g., 401, 403, or token issues) or need additional features (e.g., handling multiple scopes, role-based access), let me know, and I’ll provide targeted assistance!