using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUPAL.Core.Application.DTOs;
using NUPAL.Core.Application.Interfaces;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace NUPAL.Core.Infrastructure.Services
{
    public class GroqResumeParsingService : IResumeParsingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<GroqResumeParsingService> _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        private const string PROMPT_TEMPLATE = """
You are a professional resume parser. Your goal is to extract ALL information from the provided resume text with 100% accuracy.
CRITICAL: 
- Extracts must be VERBATIM. Do NOT summarize. Do NOT change phrasing. Do NOT shorten.
- If ANY section (like Experience, Education, Projects) is MISSING from the resume, you MUST return an empty array [] or null for it. DO NOT hallucinate or pull text from other sections.
- For the 'projects[].description', copy the entire text as it appears.
- For 'experience[].bullets', copy each bullet point exactly as written.
- Return ONLY a valid JSON object — no markdown, no explanation, just raw JSON.

Return this exact JSON structure (use null for missing fields, empty arrays [] for missing lists):
{
  "firstName": "string or null (Extract ONLY the first name. If NO name is found, return null. DO NOT use section headers like 'Professional Summary')",
  "lastName": "string or null (Extract ONLY the last name. If NO name is found, return null. DO NOT use section headers)",
  "email": "string or null",
  "phone": "string or null",
  "location": "string or null",
  "linkedIn": "string or null",
  "gitHub": "string or null",
  "website": "string or null",
  "summary": "string or null. Extract ONLY the text explicitly written under the 'Summary' or 'Profile' section. STOP at the next heading. If the section is empty or missing, return null. DO NOT summarize the resume yourself.",
  "technicalSkills": ["skill1", "skill2"],
  "softSkills": ["skill1", "skill2"],
  "experience": [
    {
      "title": "string or null",
      "company": "string or null",
      "location": "string or null",
      "startDate": "string or null",
      "endDate": "string or null",
      "isCurrent": false,
      "bullets": ["bullet1", "bullet2"]
    }
  ],
  "education": [
    {
      "degree": "string or null",
      "field": "string or null",
      "institution": "string or null",
      "location": "string or null",
      "startDate": "string or null",
      "endDate": "string or null",
      "gpa": "string or null"
    }
  ],
  "projects": [
    {
      "name": "string or null",
      "description": "CRITICAL: COPY-PASTE character-for-character the entire project description. Do NOT rephrase. Do NOT clean up. Do NOT shorten. Include every detail found.",
      "technologies": ["tech1", "tech2"],
      "link": "string or null"
    }
  ],
  "certifications": ["cert1"],
  "languages": ["Arabic", "English"],
  "awards": ["award1"]
}

RESUME TEXT:
[RESUME_TEXT]
""";

        public GroqResumeParsingService(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<GroqResumeParsingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public async Task<ParsedResumeDto> ParseResumeAsync(string resumeText, CancellationToken ct = default)
        {
            var groqApiKey = _config["GroqApiKey"]
                ?? throw new InvalidOperationException("GroqApiKey is not configured.");

            // Increased limit to 30,000 to ensure late sections (projects) aren't cut off
            var truncated = resumeText.Length > 30000 ? resumeText[..30000] : resumeText;

            var prompt = PROMPT_TEMPLATE.Replace("[RESUME_TEXT]", truncated);

            var requestBody = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.0,
                max_tokens = 4096,
                response_format = new { type = "json_object" }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", groqApiKey);
            request.Content = JsonContent.Create(requestBody);

            _logger.LogInformation("Sending resume text to Groq API for parsing. Text length: {Length}", truncated.Length);
            
            var response = await _httpClientFactory.CreateClient().SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Groq API error {response.StatusCode}: {responseBody}");

            // Extract the content field from Groq's response
            var groqResponse = JsonNode.Parse(responseBody);
            var messageContent = groqResponse?["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
                ?? throw new InvalidOperationException("Invalid response from Groq API");

            // Parse the JSON returned by LLaMA
            var parsed = JsonSerializer.Deserialize<ParsedResumeDto>(messageContent, _jsonOpts)
                ?? throw new InvalidOperationException("Failed to deserialize parsed resume.");

            // Cleanly reconstruct FullName avoiding any extraneous metadata
            var combinedName = $"{parsed.FirstName} {parsed.LastName}".Trim();
            parsed.FullName = string.IsNullOrWhiteSpace(combinedName) ? parsed.FullName : combinedName;

            // Optional general cleanup of typical LLM artifacts
            if (!string.IsNullOrWhiteSpace(parsed.FullName)) {
                parsed.FullName = parsed.FullName.Replace("|", "").Trim(' ', ',');
            }

            return parsed;
        }
    }
}

