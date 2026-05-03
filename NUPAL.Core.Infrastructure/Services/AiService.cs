using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NUPAL.Core.Application.DTOs;
using NUPAL.Core.Application.Interfaces;

namespace Nupal.Core.Infrastructure.Services
{
    public class AiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public AiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _baseUrl = config["CareerServices:Url"] ?? "http://localhost:8000";
        }

        public async Task<BlockDto> ParseSchedulePdfAsync(Stream pdfStream)
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(pdfStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(streamContent, "file", "schedule.pdf");

            var response = await _httpClient.PostAsync($"{_baseUrl.TrimEnd('/')}/v1/schedule/parse", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Career Services Parsing failed: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var block = JsonSerializer.Deserialize<BlockDto>(json, options);

            return block ?? new BlockDto { Semester = "Fall 2025", Courses = new List<CourseSessionDto>() };
        }

        // Keep compatibility with interface if needed, or update interface
        public async Task<BlockDto> ParseScheduleTextAsync(string rawText) => throw new NotImplementedException("Use ParseSchedulePdfAsync");
    }
}

