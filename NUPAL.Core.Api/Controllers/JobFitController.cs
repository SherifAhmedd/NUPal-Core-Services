using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nupal.Domain.Entities;
using NUPAL.Core.Application.Interfaces;
using NUPAL.Core.Application.DTOs;

namespace NUPAL.Core.API.Controllers
{
    [ApiController]
    [Route("api/resume/job-fit")]
    [Authorize]
    public class JobFitController : ControllerBase
    {
        private readonly IJobFitService _jobFitService;
        private readonly IResumeRepository _resumeRepository;
        private readonly IJobFitRepository _jobFitRepository;
        private readonly ILogger<JobFitController> _logger;

        public JobFitController(
            IJobFitService jobFitService,
            IResumeRepository resumeRepository,
            IJobFitRepository jobFitRepository,
            ILogger<JobFitController> logger)
        {
            _jobFitService = jobFitService;
            _resumeRepository = resumeRepository;
            _jobFitRepository = jobFitRepository;
            _logger = logger;
        }

        // POST /api/resume/job-fit/analyze
        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeFit([FromBody] AnalyzeFitRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.JobUrl))
                return BadRequest(new { error = "Job URL is required." });

            try
            {
                var email = User.Identity?.Name;
                if (string.IsNullOrEmpty(email)) return Unauthorized();

                var history = await _resumeRepository.GetByStudentEmailAsync(email);
                var latest = string.IsNullOrEmpty(request.ResumeId)
                    ? history.FirstOrDefault()
                    : history.FirstOrDefault(h => h.Id.ToString() == request.ResumeId);

                if (latest == null)
                    return BadRequest(new { error = "No resume found for analysis. Please upload your resume first." });

                var analysis = await _jobFitService.AnalyzeFitAsync(request.JobUrl, latest.Data, ct);

                // Persist to DB
                var record = new JobFitResult
                {
                    StudentEmail = email,
                    JobUrl = request.JobUrl,
                    AnalyzedAt = DateTime.UtcNow,
                    AnalysisJson = System.Text.Json.JsonSerializer.Serialize(analysis)
                };
                await _jobFitRepository.SaveAsync(record);

                return Ok(new { id = record.Id.ToString(), analysis });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Job Fit Analysis");
                return StatusCode(500, new { error = "Fit analysis failed", message = ex.Message });
            }
        }

        // GET /api/resume/job-fit/history
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var results = await _jobFitRepository.GetByStudentEmailAsync(email);
            var summary = results.Select(r => {
                var analysis = System.Text.Json.JsonSerializer.Deserialize<JobFitAnalysisDto>(r.AnalysisJson);
                return new
                {
                    id          = r.Id.ToString(),
                    jobTitle    = analysis?.JobTitle,
                    companyName = analysis?.CompanyName,
                    overallScore= analysis?.OverallScore ?? 0,
                    matchStatus = analysis?.MatchStatus,
                    jobUrl      = r.JobUrl,
                    analyzedAt  = r.AnalyzedAt
                };
            });

            return Ok(summary);
        }

        // GET /api/resume/job-fit/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var record = await _jobFitRepository.GetByIdAsync(id);
            if (record == null) return NotFound();
            if (record.StudentEmail != email) return Forbid();

            var analysis = System.Text.Json.JsonSerializer.Deserialize<JobFitAnalysisDto>(record.AnalysisJson);
            return Ok(new { id = record.Id.ToString(), analysis = analysis, jobUrl = record.JobUrl, analyzedAt = record.AnalyzedAt });
        }

        // DELETE /api/resume/job-fit/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var record = await _jobFitRepository.GetByIdAsync(id);
            if (record == null) return NotFound();
            if (record.StudentEmail != email) return Forbid();

            await _jobFitRepository.DeleteAsync(id);
            return NoContent();
        }
    }

    public class AnalyzeFitRequest
    {
        public string JobUrl { get; set; } = string.Empty;
        public string? ResumeId { get; set; }
    }
}
