using AspNetCore.Swagger.Themes;
using Microsoft.OpenApi.Models;
using WebAPI.Services;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Configure Swagger to use API Key authentication
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key authentication using the X-API-KEY header",
        Name = "X-API-KEY",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    var scheme = new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "ApiKey"
        },
        In = ParameterLocation.Header
    };

    var requirement = new OpenApiSecurityRequirement
    {
        { scheme, new List<string>() }
    };

    c.AddSecurityRequirement(requirement);
});

// Register AuthService
builder.Services.AddScoped<AuthService>();

//Lock down all APIs to require API Key authentication
//builder.Services.AddControllers(options =>
//{
//    options.Filters.AddService<AuthService>();
//});

var app = builder.Build();

// Add middleware to redirect the root URL to Swagger UI
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger");
        return;
    }
    await next();
});

app.UseSwagger(c =>
{
    c.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0;
});

app.UseSwaggerUI(ModernStyle.Dark);
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
