using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nupal.Domain.Entities;
using NUPAL.Core.Application.Interfaces;

namespace NUPAL.Core.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/career-data")]
    public class CareerDataController : ControllerBase
    {
        private readonly IResumeRepository _resumeRepository;
        private readonly IJobFitRepository _jobFitRepository;

        public CareerDataController(IResumeRepository resumeRepository, IJobFitRepository jobFitRepository)
        {
            _resumeRepository = resumeRepository;
            _jobFitRepository = jobFitRepository;
        }

        [HttpPost("resume-analyses")]
        public async Task<IActionResult> CreateResumeAnalysis(
            [FromQuery] string studentEmail,
            [FromBody] CreateResumeAnalysisRequest request)
        {
            if (string.IsNullOrWhiteSpace(studentEmail))
                return BadRequest(new { detail = "studentEmail is required" });
            if (request == null || string.IsNullOrWhiteSpace(request.FileName))
                return BadRequest(new { detail = "fileName is required" });
            if (!OwnsStudentEmail(studentEmail))
                return Forbid();

            var model = new ResumeAnalysis
            {
                StudentEmail = studentEmail.Trim(),
                FileName = request.FileName.Trim(),
                Data = request.Data ?? new ResumeData(),
                AnalyzedAt = DateTime.UtcNow
            };

            await _resumeRepository.SaveAsync(model);
            return Ok(new { id = model.Id.ToString() });
        }

        [HttpGet("resume-analyses")]
        public async Task<IActionResult> ListResumeAnalyses([FromQuery] string studentEmail)
        {
            if (string.IsNullOrWhiteSpace(studentEmail))
                return BadRequest(new { detail = "studentEmail is required" });
            if (!OwnsStudentEmail(studentEmail))
                return Forbid();

            var rows = await _resumeRepository.GetByStudentEmailAsync(studentEmail.Trim());
            return Ok(rows);
        }

        [HttpGet("resume-analyses/{id}")]
        public async Task<IActionResult> GetResumeAnalysis([FromRoute] string id, [FromQuery] string studentEmail)
        {
            if (string.IsNullOrWhiteSpace(studentEmail))
                return BadRequest(new { detail = "studentEmail is required" });
            if (!OwnsStudentEmail(studentEmail))
                return Forbid();

            var row = await _resumeRepository.GetByIdAsync(id);
            if (row == null || !EmailEquals(row.StudentEmail, studentEmail))
                return NotFound(new { detail = "Resume not found" });

            return Ok(row);
        }

        [HttpDelete("resume-analyses/{id}")]
        public async Task<IActionResult> DeleteResumeAnalysis([FromRoute] string id, [FromQuery] string studentEmail)
        {
            if (string.IsNullOrWhiteSpace(studentEmail))
                return BadRequest(new { detail = "studentEmail is required" });
            if (!OwnsStudentEmail(studentEmail))
                return Forbid();

            var row = await _resumeRepository.GetByIdAsync(id);
            if (row == null || !EmailEquals(row.StudentEmail, studentEmail))
                return NotFound(new { detail = "Resume not found" });

            await _resumeRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("job-fit-results")]
        public async Task<IActionResult> CreateJobFitResult(
            [FromQuery] string studentEmail,
            [FromBody] CreateJobFitResultRequest request)
        {
            if (string.IsNullOrWhiteSpace(studentEmail))
                return BadRequest(new { detail = "studentEmail is required" });
            if (request == null)
                return BadRequest(new { detail = "request body is required" });
            if (!OwnsStudentEmail(studentEmail))
                return Forbid();

            var model = new JobFitResult
            {
                StudentEmail = studentEmail.Trim(),
                JobUrl = request.JobUrl ?? string.Empty,
                JobText = request.JobText ?? string.Empty,
                AnalysisJson = request.AnalysisJson ?? "{}",
                AnalyzedAt = DateTime.UtcNow
            };

            await _jobFitRepository.SaveAsync(model);
            return Ok(new { id = model.Id.ToString() });
        }

        [HttpGet("job-fit-results")]
        public async Task<IActionResult> ListJobFitResults([FromQuery] string studentEmail)
        {
            if (string.IsNullOrWhiteSpace(studentEmail))
                return BadRequest(new { detail = "studentEmail is required" });
            if (!OwnsStudentEmail(studentEmail))
                return Forbid();

            var rows = await _jobFitRepository.GetByStudentEmailAsync(studentEmail.Trim());
            return Ok(rows);
        }

        [HttpGet("job-fit-results/{id}")]
        public async Task<IActionResult> GetJobFitResult([FromRoute] string id, [FromQuery] string studentEmail)
        {
            if (string.IsNullOrWhiteSpace(studentEmail))
                return BadRequest(new { detail = "studentEmail is required" });
            if (!OwnsStudentEmail(studentEmail))
                return Forbid();

            var row = await _jobFitRepository.GetByIdAsync(id);
            if (row == null || !EmailEquals(row.StudentEmail, studentEmail))
                return NotFound(new { detail = "Job fit not found" });

            return Ok(row);
        }

        [HttpDelete("job-fit-results/{id}")]
        public async Task<IActionResult> DeleteJobFitResult([FromRoute] string id, [FromQuery] string studentEmail)
        {
            if (string.IsNullOrWhiteSpace(studentEmail))
                return BadRequest(new { detail = "studentEmail is required" });
            if (!OwnsStudentEmail(studentEmail))
                return Forbid();

            var row = await _jobFitRepository.GetByIdAsync(id);
            if (row == null || !EmailEquals(row.StudentEmail, studentEmail))
                return NotFound(new { detail = "Job fit not found" });

            await _jobFitRepository.DeleteAsync(id);
            return NoContent();
        }

        private bool OwnsStudentEmail(string studentEmail)
        {
            var fromToken = UserEmailFromClaims();
            if (string.IsNullOrWhiteSpace(fromToken))
                return false;
            return EmailEquals(fromToken, studentEmail);
        }

        private string UserEmailFromClaims()
        {
            return User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("email")
                ?? User.FindFirstValue("unique_name")
                ?? User.Identity?.Name
                ?? string.Empty;
        }

        private static bool EmailEquals(string a, string b)
        {
            return string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }

    public class CreateResumeAnalysisRequest
    {
        public string FileName { get; set; } = string.Empty;
        public ResumeData Data { get; set; } = new();
    }

    public class CreateJobFitResultRequest
    {
        public string JobUrl { get; set; } = string.Empty;
        public string JobText { get; set; } = string.Empty;
        public string AnalysisJson { get; set; } = "{}";
    }
}
