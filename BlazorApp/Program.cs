using BlazorApp.Components;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// ################################################ AD B2C configuration begin ################################################
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAdB2C"));
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();

//.NET 8 Upgrade Causes Post-Logout Loop in Blazor Server App with Azure AD B2C
builder.Services.AddRazorPages(); //https://github.com/dotnet/aspnetcore/issues/52245
// ################################################ AD B2C configuration end ################################################

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Starting BlazorApp...");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Since IdentityModel version 5.2.1 (or since Microsoft.AspNetCore.Authentication.JwtBearer version 2.2.0),
    // PII hiding in log files is enabled by default for GDPR concerns.
    // For debugging/development purposes, one can enable additional detail in exceptions by setting IdentityModelEventSource.ShowPII to true.
    // Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// ################################################ AD B2C configuration begin ################################################
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// ################################################ AD B2C configuration end ################################################

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

logger.LogInformation("BlazorApp started successfully.");

app.Run();

