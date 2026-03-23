using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nupal.Domain.Entities;
using NUPAL.Core.Application.DTOs;
using NUPAL.Core.Application.Interfaces;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Nupal.Core.Infrastructure.Services
{
    public class JobFitService : IJobFitService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<JobFitService> _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        private const string JD_EXTRACTION_PROMPT = """
        You are a precise technical requirements extractor. Your ONLY job is to read the Job Description below and extract information WITHOUT losing or omitting any detail.

        CRITICAL RULES:
        - Copy EVERY technology, tool, language, framework, library, platform, methodology, and skill EXACTLY as written in the JD. Do NOT paraphrase, merge, or drop any keyword.
        - List ALL requirements even if they seem minor or repeated.
        - Preserve the exact names (e.g. "React.js" not "React", "PostgreSQL" not "SQL").
        - Separate must-have from nice-to-have if explicitly stated; otherwise list everything under requirements.
        - Include experience level, education requirements, soft skills, domain context, and any tools/processes mentioned.

        OUTPUT FORMAT:
        --- REQUIRED SKILLS & TECHNOLOGIES (list every single one) ---
        [exhaustive comma-separated list]

        --- EXPERIENCE & SENIORITY ---
        [exact wording from JD]

        --- EDUCATION REQUIREMENTS ---
        [exact wording from JD]

        --- DOMAIN / INDUSTRY CONTEXT ---
        [company domain, industry, product type]

        --- RESPONSIBILITIES ---
        [bullet list of all responsibilities mentioned]

        --- NICE-TO-HAVE / PREFERRED ---
        [list if mentioned, else write "Not specified"]

        Job Description:
        [JOB_TEXT]
        """;

        private const string JOB_FIT_PROMPT = """
You are an expert career analyst and talent evaluator with experience across ALL industries, 
roles, and seniority levels — from internships to executive positions, 
from software engineering to marketing, finance, design, operations, and beyond.

YOUR MISSION:
Carefully read the provided CV and Job Description, then produce a thorough, honest, 
and highly specific Job Fit Analysis. You are NOT filling a template blindly — 
you are THINKING like a senior hiring manager who has read thousands of CVs.

═══════════════════════════════════
HOW TO THINK (internal reasoning)
═══════════════════════════════════

STEP 1 — Understand the job deeply.
  What is this role actually asking for? What does success look like on Day 1, Month 3, Year 1?
  What are the must-haves vs. nice-to-haves? What industry context matters?
  What level of seniority, ownership, and communication is expected?

STEP 2 — Understand the candidate deeply.
  What is this person's actual level? What have they truly done vs. merely listed?
  What patterns emerge from their trajectory — are they growing, pivoting, specializing?
  What can be inferred from dates, titles, projects, and achievements?

STEP 3 — Compare honestly.
  Where do they genuinely align? Be specific — cite actual skills, projects, or experiences.
  Where do they fall short? Be precise — name the exact requirement and the exact gap.
  Is the gap a dealbreaker or something bridgeable? How quickly?
  Is the candidate overqualified? That's also a risk worth mentioning.

STEP 4 — Score fairly and CONSISTENTLY.
  The score must reflect reality, not encouragement. Scores must be deterministic.
  Given identical inputs, you MUST always produce the same score.
  Use ONLY this strict weighted formula — do not deviate:
    - Skills match:         30%  → count matched vs total JD keywords
    - Experience match:     30%  → compare years/level required vs candidate actual
    - Domain/Industry fit:  20%  → how closely does the candidate's background match this industry?
    - Credentials:          10%  → education level required vs candidate's current status
    - Day-1 Readiness:      10%  → can they contribute from day one?
  Calculate each sub-score first (0-100), then apply weights:
  overallScore = round((skills×0.3) + (experience×0.3) + (domain×0.2) + (credentials×0.1) + (readiness×0.1))
  A weak fit WILL score 30-40. A strong fit WILL score 85-95. Both are valid. Be accurate.

STEP 5 — Give actionable, specific advice.
  Every recommendation must address a real gap you identified.
  Every interview tip must be tied to something real in the JD or CV.
  No generic advice. "Build projects" is useless. 
  "Build a REST API project using [specific tech from JD] to demonstrate production-level 
  experience you currently lack" is useful.

═══════════════════════════════════
INPUTS
═══════════════════════════════════

Candidate CV/Resume (JSON):
[RESUME_DATA]

Job Description:
[JOB_TEXT]

═══════════════════════════════════
OUTPUT — RETURN ONLY THIS JSON
═══════════════════════════════════

{
You are an expert Technical Recruiter and Career Coach. Your task is to perform a DEEP, ANALYTICAL Job Fit Analysis between a Candidate's CV and a Job Description (JD).

INPUT DATA:
- CANDIDATE CV (JSON): [RESUME_DATA]
- JOB DESCRIPTION (REQS): [JOB_TEXT]

STRICT ANALYSIS RULES:
1. SENIORITY & EDUCATION CHECK: 
   - Look at the Education section. If the graduation date is in the future (e.g., 2026 or 2027) or 'isCurrent' is true for a degree, the candidate is a STUDENT.
   - If the JD requires a "Bachelor's Degree", "Graduate", or "X years of professional experience" and the candidate is still a student, this is a CRITICAL GAP (Red Flag).
   - Flag any seniority mismatch (e.g., Student applying for Senior/Lead role).

2. EXPERIENCE CALCULATION RULES (CRITICAL):
   - If Candidate is a student or fresh grad, their full-time professional experience is 0 YEARS.
   - Do NOT count academic projects, part-time student activities, or short-term summer internships as full-time professional years of experience.
   - For example, if a JD asks for "0-2 years experience" and the candidate is a student, clearly state their professional experience is 0 years but they have internship/project exposure. Do NOT say "Candidate experience is 1 year". State exactly what they have (e.g., "You are a student with 0 years full-time professional experience").

3. ANTI-REPETITION POLICY:
   - EACH field in the JSON must provide UNIQUE value. Do NOT repeat the same insight across 'detailedSummary', 'highlights', 'opportunities', and 'recommendations'.
   - If you mention a gap in 'detailedSummary', explain its TECHNICAL IMPACT in 'opportunities' and provide a FIX in 'recommendations'. Do not just restate the gap.

4. DATA-DRIVEN INSIGHTS:
   - Use specific technologies, years, and metrics from both the CV and JD.
   - Be objective and critical. If the fit is 30%, explain exactly why without being discouraging but without sugarcoating.

5. TONE & PERSPECTIVE (CRITICAL):
   - You MUST speak DIRECTLY to the user as if you are having a 1-on-1 mentoring session with them.
   - Use "you", "your CV", "your experience" instead of "the candidate", "the applicant", or "they".
   - Every single field in the JSON (including notes, flags, gaps, and summaries) must be phrased as direct feedback to the user (e.g., "You have 0 years of experience", "Your degree is a strong match", "You need to focus on X").

FIELD RULES (follow strictly before writing JSON):
- matchedSkills: List EVERY keyword/technology/tool/framework/skill that appears in BOTH the JD and the CV. Zero exceptions. Do NOT group or summarize. Include ALL of them.
- missingSkills: List EVERY keyword/technology/tool/framework/skill in the JD that does NOT appear in the CV. Zero exceptions. Do NOT group or summarize. Include ALL of them.
- skillsNote: Format EXACTLY as 'LLM reviewed [Total] priority keywords and confirmed [Matched] as covered.' — numbers must match the actual lengths of matchedSkills and missingSkills arrays.
- interviewFocus: Each item is a specific tip tied to real content in the JD or CV. Format: 'Prepare to explain [CV topic] in context of [JD requirement].' Be concrete.
- suggestedLearning: Each item must name a SPECIFIC resource (course + platform + time estimate). Never vague.
- redFlags: Short, plain sentence per concern. Empty array [] if none.
- recommendations: One paragraph per Critical/High missing skill. Name the skill, explain its role context, give exact resources, suggest a micro-project, estimate score impact.

RETURN ONLY VALID JSON:
{
  "jobTitle": "Official Job Title from JD",
  "companyName": "Company Name from JD",
  "overallScore": 0,
  "detailedSummary": "3-5 sentences speaking directly to the candidate using 'you'/'your', like a career mentor. Vary the opening angle each time.",

  "breakdown": {
    "skills":      0,
    "experience":  0,
    "domain":      0,
    "credentials": 0,
    "readiness":   0,

    "skillsNote":      "LLM reviewed X priority keywords and confirmed Y as covered.",
    "experienceNote":  "Your exact professional experience vs JD requirement.",
    "domainNote":      "Your industry/domain alignment.",
    "credentialsNote": "Your degree status vs JD requirements.",

    "matchedSkills": [
      { "skill": "Exact keyword from JD", "evidence": "Where it appears in CV", "level": "Exposure | Practical | Advanced" }
    ],
    "missingSkills": [
      { "skill": "Exact keyword from JD", "importance": "Critical | High | Medium | Low", "fixable": "Specific short strategy" }
    ]
  },

  "highlights": [
    "Unique strength 1",
    "Unique strength 2",
    "Unique strength 3"
  ],

  "opportunities": [
    "Major gap 1 with why it matters",
    "Major gap 2",
    "Major gap 3"
  ],

  "redFlags": [
    "Short plain sentence about concern and why it's risky. Empty array if none."
  ],

  "recommendations": [
    "One focused paragraph per Critical/High missing skill."
  ],

  "actionPlan": [
    { "targetGap": "Gap name", "expectedImpact": "Impact description", "priority": "Critical | High | Medium | Low", "status": "Do now | Do soon | Do later" }
  ],

  "interviewFocus": [
    "Specific preparation tip tied to real JD/CV content."
  ],

  "suggestedLearning": [
    "Specific resource name on platform — addresses gap in skill. Estimated time: X hours."
  ]
}

STRICT RULES:
- Return ONLY raw JSON. No markdown. No text outside the JSON.
- Never be generic. Every field must reference specific details from the CV or JD.
- If a field has no relevant data, infer intelligently and note the inference.
- Adapt your language and criteria to the role type — a creative role is judged differently than an engineering role.
""";

        public JobFitService(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<JobFitService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public async Task<JobFitAnalysisDto> AnalyzeFitAsync(string jobUrl, ResumeData resumeData, CancellationToken ct)
        {
            _logger.LogInformation("Starting Job Fit Analysis for URL: {Url}", jobUrl);

            // 1. Scraping: extract text from URL
            string rawJobDescription = await ExtractJobDescriptionFromUrlAsync(jobUrl, ct);
            if (string.IsNullOrWhiteSpace(rawJobDescription))
            {
                throw new Exception("Could not extract any text from the provided job link. Please ensure it is a public job posting.");
            }

            // 2. Step 1: Extract/Summarize JD using Light Model (8B)
            _logger.LogInformation("Step 1: Extracting JD requirements using light model.");
            var jdExtractionPrompt = JD_EXTRACTION_PROMPT.Replace("[JOB_TEXT]", rawJobDescription.Length > 10000 ? rawJobDescription[..10000] : rawJobDescription);
            var extractedJd = await CallGroqAsync(jdExtractionPrompt, "llama-3.1-8b-instant", false, ct);

            // 3. Step 2: Job Fit Analysis using Heavy Model (70B)
            _logger.LogInformation("Step 2: Performing Job Fit Analysis using heavy model.");
            var resumeJson = JsonSerializer.Serialize(resumeData);
            var finalPrompt = JOB_FIT_PROMPT
                .Replace("[RESUME_DATA]", resumeJson)
                .Replace("[JOB_TEXT]", extractedJd);

            var analysisContent = await CallGroqAsync(finalPrompt, "llama-3.3-70b-versatile", true, ct);

            var analysis = JsonSerializer.Deserialize<JobFitAnalysisDto>(analysisContent, _jsonOpts)
                ?? throw new InvalidOperationException("Failed to deserialize job fit analysis.");

            return analysis;
        }

        private async Task<string> CallGroqAsync(string prompt, string model, bool isJsonObject, CancellationToken ct)
        {
            var groqApiKey = _config["GroqApiKey"] ?? throw new InvalidOperationException("GroqApiKey is not configured.");
            var requestBody = new
            {
                model = model,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0,
                seed = 42,
                max_tokens = 8192,
                response_format = isJsonObject ? new { type = "json_object" } : null
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", groqApiKey);
            request.Content = JsonContent.Create(requestBody, mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));

            var response = await _httpClientFactory.CreateClient().SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Groq API error {response.StatusCode}: {responseBody}");

            var groqResponse = JsonNode.Parse(responseBody);
            return groqResponse?["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
                ?? throw new InvalidOperationException("Invalid response from Groq API");
        }

        private async Task<string> ExtractJobDescriptionFromUrlAsync(string url, CancellationToken ct)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                // Add common User-Agent to avoid blocking
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                
                var html = await client.GetStringAsync(url, ct);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Basic cleaning: remove scripts, styles, etc.
                var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//style|//nav|//header|//footer|//iframe");
                if (nodesToRemove != null)
                {
                    foreach (var node in nodesToRemove) node.Remove();
                }

                // Try to find the "main" content or just get all text
                var mainNode = doc.DocumentNode.SelectSingleNode("//main|//article|//div[contains(@class, 'job')]|//div[contains(@class, 'description')]");
                string rawText = mainNode != null ? mainNode.InnerText : doc.DocumentNode.InnerText;

                // Clean whitespace
                return System.Net.WebUtility.HtmlDecode(rawText).Replace("\n", " ").Replace("\r", " ").Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scrape URL: {Url}", url);
                return string.Empty;
            }
        }
    }
}
