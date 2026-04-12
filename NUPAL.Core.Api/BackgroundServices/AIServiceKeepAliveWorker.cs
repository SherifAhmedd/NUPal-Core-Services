using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace NUPAL.Core.Api.BackgroundServices;

public class AIServiceKeepAliveWorker : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIServiceKeepAliveWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

    public AIServiceKeepAliveWorker(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AIServiceKeepAliveWorker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Service Keep-Alive Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var rlServiceUrl = _configuration["RlServiceUrl"];
                var agentServiceUrl = _configuration["AgentServiceUrl"];

                if (!string.IsNullOrEmpty(rlServiceUrl))
                {
                    await PingService(rlServiceUrl, "RL Service", stoppingToken);
                }

                if (!string.IsNullOrEmpty(agentServiceUrl))
                {
                    await PingService(agentServiceUrl, "Agent Service", stoppingToken);
                }

                var careerServiceUrl = _configuration["CareerServices:Url"];
                if (!string.IsNullOrEmpty(careerServiceUrl))
                {
                    await PingService(careerServiceUrl, "Career Services", stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while pinging AI services.");
            }

            _logger.LogInformation("Waiting for {Interval} before next ping.", _interval);
            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("AI Service Keep-Alive Worker is stopping.");
    }

    private async Task PingService(string url, string serviceName, CancellationToken ct)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            
            _logger.LogInformation("Pinging {ServiceName} at {Url}...", serviceName, url);
            var response = await client.GetAsync(url, ct);
            
            _logger.LogInformation("{ServiceName} responded with {StatusCode}", serviceName, response.StatusCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Failed to ping {ServiceName} at {Url}: {Message}", serviceName, url, ex.Message);
        }
    }
}
