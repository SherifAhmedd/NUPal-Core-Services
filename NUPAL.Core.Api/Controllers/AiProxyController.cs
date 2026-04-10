using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NUPAL.Core.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ai-proxy")]
public class AiProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiProxyController> _logger;

    // Headers managed by Kestrel — must never be set manually
    private static readonly HashSet<string> _restrictedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host", "Transfer-Encoding", "Content-Length", "Content-Encoding",
        "Connection", "Keep-Alive", "Upgrade", "Proxy-Connection"
    };

    public AiProxyController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AiProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [Route("{*path}")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    public async Task Proxy(string path)
    {
        var baseUrl = _configuration["CareerServices:Url"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            _logger.LogError("CareerServices:Url is not configured.");
            Response.StatusCode = 502;
            await Response.WriteAsJsonAsync(new
            {
                success = false,
                error = "Proxy misconfiguration",
                detail = "CareerServices:Url is not set. Add CareerServices__Url (double underscore) to Azure App Service environment variables."
            });
            return;
        }

        var targetUri = new Uri($"{baseUrl}/{path}{Request.QueryString}");
        _logger.LogInformation("Proxying {Method} /{Path} => {Target}", Request.Method, path, targetUri);

        using var proxyRequest = new HttpRequestMessage(new HttpMethod(Request.Method), targetUri);

        // Always forward the body for methods that can carry one (POST, PUT, PATCH, DELETE with body).
        // DO NOT check ContentLength — Azure App Service can set this to null for large uploads.
        // DO NOT set Content-Length manually — HttpClient will compute it correctly from the StreamContent.
        if (!HttpMethods.IsGet(Request.Method) && !HttpMethods.IsHead(Request.Method))
        {
            var streamContent = new StreamContent(Request.Body);
            if (!string.IsNullOrEmpty(Request.ContentType))
            {
                streamContent.Headers.TryAddWithoutValidation("Content-Type", Request.ContentType);
            }
            proxyRequest.Content = streamContent;
        }

        // Forward all request headers except restricted ones and Content-Type (already on Content headers)
        foreach (var header in Request.Headers)
        {
            if (_restrictedHeaders.Contains(header.Key)
                || header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                continue;

            proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        // Inject internal service API key for the Python backend
        var apiKey = _configuration["CareerServices:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            proxyRequest.Headers.Remove("X-API-Key");
            proxyRequest.Headers.TryAddWithoutValidation("X-API-Key", apiKey);
        }

        // Use the named client with a 3-minute timeout (handles HF Space cold starts + LLM latency)
        var httpClient = _httpClientFactory.CreateClient("CareerServicesProxy");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(
                proxyRequest,
                HttpCompletionOption.ResponseHeadersRead,
                HttpContext.RequestAborted);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI backend unreachable at {Target}", targetUri);
            Response.StatusCode = 502;
            await Response.WriteAsJsonAsync(new { success = false, error = "AI service unreachable", detail = ex.Message });
            return;
        }
        catch (TaskCanceledException ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout reaching AI backend at {Target}", targetUri);
            Response.StatusCode = 504;
            await Response.WriteAsJsonAsync(new { success = false, error = "AI service timed out", detail = "The AI backend did not respond in time. Please try again." });
            return;
        }

        using (response)
        {
            Response.StatusCode = (int)response.StatusCode;

            foreach (var header in response.Headers)
            {
                if (!_restrictedHeaders.Contains(header.Key))
                    Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in response.Content.Headers)
            {
                if (!_restrictedHeaders.Contains(header.Key))
                    Response.Headers[header.Key] = header.Value.ToArray();
            }

            await response.Content.CopyToAsync(Response.Body, HttpContext.RequestAborted);
        }
    }
}
