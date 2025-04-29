using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace WebAPI.Services;

/// <summary>
/// Provides authentication services by validating API keys for incoming requests.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AuthService"/> class.
/// </remarks>
/// <param name="configuration">The configuration to access API key settings.</param>
/// <param name="logger">The logger instance for logging authorization events.</param>
/// <exception cref="ArgumentNullException">Thrown when settings or logger is null.</exception>
public sealed class AuthService(IConfiguration configuration, ILogger<AuthService> logger) : IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-API-KEY";
    private readonly ILogger<AuthService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _apiKey = configuration?.GetValue<string>("ApiKey") ?? throw new ArgumentNullException(nameof(configuration), "API Key configuration is missing");

    /// <summary>
    /// Executes authorization by validating the API key in the request headers.
    /// </summary>
    /// <param name="context">The context for the action being executed, including request headers.</param>
    /// <param name="next">The delegate to execute the next action if authorization is successful.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            _logger.LogWarning("Authorization failed: API key was not provided.");
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!string.Equals(_apiKey, extractedApiKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Authorization failed: Invalid API key provided.");
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}

