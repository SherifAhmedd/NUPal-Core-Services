using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NUPAL.Core.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ai-proxy")]
public class AiProxyController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AiProxyController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
    }

    [Route("{*path}")]
    public async Task Proxy(string path)
    {
        var baseUrl = _configuration["CareerServices:Url"]?.TrimEnd('/');
        var targetUri = new Uri($"{baseUrl}/{path}{Request.QueryString}");

        using var proxyRequest = new HttpRequestMessage(new HttpMethod(Request.Method), targetUri);

        if (Request.ContentLength > 0 || Request.Headers.TransferEncoding.Count > 0)
        {
            // Use StreamContent without buffering
            proxyRequest.Content = new StreamContent(Request.Body);
            
            if (Request.ContentType != null)
            {
                proxyRequest.Content.Headers.TryAddWithoutValidation("Content-Type", Request.ContentType);
            }
        }

        // Copy everything over (including the all-important Authorization: Bearer token for AI)
        foreach (var header in Request.Headers)
        {
            if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        // Inject the hidden backend API Key so FastAPI knows it's an authorized internal call
        var apiKey = _configuration["CareerServices:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            proxyRequest.Headers.TryAddWithoutValidation("X-API-Key", apiKey);
        }

        using var response = await _httpClient.SendAsync(proxyRequest, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
        
        Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            Response.Headers[header.Key] = header.Value.ToArray();
        }
        
        Response.Headers.Remove("transfer-encoding");

        await response.Content.CopyToAsync(Response.Body, HttpContext.RequestAborted);
    }
}
