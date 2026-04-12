using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace NUPAL.Core.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ai-proxy")]
public class AiProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiProxyController> _logger;

    private static readonly HashSet<string> _restrictedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host", "Transfer-Encoding", "Content-Length", "Content-Encoding",
        "Connection", "Keep-Alive", "Upgrade", "Proxy-Connection",
        "Origin", "Referer", "Cookie", "Set-Cookie",
        "Access-Control-Allow-Origin", "Access-Control-Allow-Credentials",
        "Access-Control-Allow-Methods", "Access-Control-Allow-Headers",
        "Access-Control-Expose-Headers", "Access-Control-Max-Age"
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
    public async Task Proxy(string path)
    {
        // Ensure the body can be read multiple times (critical if middleware already read it)
        Request.EnableBuffering();
        if (Request.Body.CanSeek)
        {
            Request.Body.Position = 0;
        }

        var baseUrl = _configuration["CareerServices:Url"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            Response.StatusCode = 502;
            await Response.WriteAsJsonAsync(new { success = false, error = "CareerServices:Url not configured" });
            return;
        }

        var targetUri = new Uri($"{baseUrl}/{path}{Request.QueryString}");
        _logger.LogInformation("Proxying {Method} /{Path} => {Target}", Request.Method, path, targetUri);

        using var proxyRequest = new HttpRequestMessage(new HttpMethod(Request.Method), targetUri);

        // Build request body
        if (!HttpMethods.IsGet(Request.Method) && !HttpMethods.IsHead(Request.Method))
        {
            var contentType = Request.ContentType ?? "";

            if (contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                // For file uploads: ASP.NET Core's multipart reader is the only reliable way
                // to read the uploaded file from Kestrel. We then repack it into a new
                // MultipartFormDataContent (HttpClient auto-generates the correct boundary).
                var form = await Request.ReadFormAsync();
                var multipart = new MultipartFormDataContent();

                // Forward any plain text fields
                foreach (var field in form)
                    foreach (var val in field.Value)
                        multipart.Add(new StringContent(val ?? ""), field.Key);

                // Forward all uploaded files
                foreach (var formFile in form.Files)
                {
                    var fileStream = formFile.OpenReadStream();
                    var fileContent = new StreamContent(fileStream);
                    if (!string.IsNullOrEmpty(formFile.ContentType))
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(formFile.ContentType);
                    multipart.Add(fileContent, formFile.Name, formFile.FileName);
                }

                proxyRequest.Content = multipart;
            }
            else
            {
                // For JSON and other bodies: stream directly
                var streamContent = new StreamContent(Request.Body);
                if (!string.IsNullOrEmpty(contentType))
                    streamContent.Headers.TryAddWithoutValidation("Content-Type", contentType);
                
                if (Request.ContentLength.HasValue)
                    streamContent.Headers.ContentLength = Request.ContentLength.Value;

                proxyRequest.Content = streamContent;
            }
        }

        // Forward request headers (skip Content-Type — already set on Content, and restricted headers)
        foreach (var header in Request.Headers)
        {
            if (_restrictedHeaders.Contains(header.Key)
                || header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                continue;
            proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        // Inject internal service API key
        var apiKey = _configuration["CareerServices:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            proxyRequest.Headers.Remove("X-API-Key");
            proxyRequest.Headers.TryAddWithoutValidation("X-API-Key", apiKey);
        }

        HttpResponseMessage? response = null;
        try
        {
            var httpClient = _httpClientFactory.CreateClient("CareerServicesProxy");
            response = await httpClient.SendAsync(
                proxyRequest,
                HttpCompletionOption.ResponseHeadersRead,
                HttpContext.RequestAborted);

            using (response)
            {
                Response.StatusCode = (int)response.StatusCode;

                foreach (var header in response.Headers)
                    if (!_restrictedHeaders.Contains(header.Key))
                        Response.Headers[header.Key] = header.Value.ToArray();

                foreach (var header in response.Content.Headers)
                    if (!_restrictedHeaders.Contains(header.Key))
                        Response.Headers[header.Key] = header.Value.ToArray();

                await response.Content.CopyToAsync(Response.Body, HttpContext.RequestAborted);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI backend unreachable at {Target}. Error: {Error}. Inner: {Inner}", targetUri, ex.Message, ex.InnerException?.Message);
            Response.StatusCode = 502;
            await Response.WriteAsJsonAsync(new 
            { 
                success = false, 
                error = "AI service unreachable", 
                detail = ex.Message,
                innerError = ex.InnerException?.Message,
                target = targetUri.ToString() 
            });
        }
        catch (TaskCanceledException ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout at {Target} after 3 minutes", targetUri);
            Response.StatusCode = 504;
            await Response.WriteAsJsonAsync(new { success = false, error = "AI service timed out", target = targetUri.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error proxying to {Target}: {Message}", targetUri, ex.Message);
            if (!Response.HasStarted)
            {
                Response.StatusCode = 500;
                await Response.WriteAsJsonAsync(new { success = false, error = "Internal proxy error", detail = ex.Message });
            }
        }
    }
}
