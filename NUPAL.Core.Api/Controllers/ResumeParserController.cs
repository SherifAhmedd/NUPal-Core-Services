using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using NUPAL.Core.Application.Interfaces;
using Nupal.Domain.Entities;
using NUPAL.Core.Application.DTOs;

namespace NUPAL.Core.API.Controllers
{
    [ApiController]
    [Route("api/resume")]
    [Authorize]
    public class ResumeParserController : ControllerBase
    {
        private readonly IPdfTextExtractorService _pdfTextExtractorService;
        private readonly IResumeParsingService _resumeParsingService;
        private readonly IResumeRepository _resumeRepository;
        private readonly ILogger<ResumeParserController> _logger;

        public ResumeParserController(
            IPdfTextExtractorService pdfTextExtractorService,
            IResumeParsingService resumeParsingService,
            IResumeRepository resumeRepository,
            ILogger<ResumeParserController> logger)
        {
            _pdfTextExtractorService = pdfTextExtractorService;
            _resumeParsingService = resumeParsingService;
            _resumeRepository = resumeRepository;
            _logger = logger;
        }

        [HttpPost("parse")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max
        public async Task<IActionResult> Parse([FromForm] IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file provided." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf")
                return BadRequest(new { error = "Only PDF files are supported." });

            try
            {
                // 1. Extract text from PDF using the text extractor service
                using var stream = file.OpenReadStream();
                string resumeText = _pdfTextExtractorService.ExtractTextFromPdf(stream);

                if (string.IsNullOrWhiteSpace(resumeText) || resumeText.Length < 50)
                    return BadRequest(new { error = "Could not extract text from the PDF. Make sure the PDF contains selectable text (not a scanned image)." });

                // 2. Call AI Parsing service for professional parsing
                var parsed = await _resumeParsingService.ParseResumeAsync(resumeText, ct);

                // 3. PERSISTENCE: Save analysis automatically for the user
                var studentEmail = User.Identity?.Name ?? "unknown";
                var analysis = new ResumeAnalysis
                {
                    StudentEmail = studentEmail,
                    FileName = file.FileName,
                    AnalyzedAt = DateTime.UtcNow,
                    Data = parsed
                };

                await _resumeRepository.SaveAsync(analysis);
                _logger.LogInformation("Resume analysis for {Email} saved with ID {Id}", studentEmail, analysis.Id);

                return Ok(new { 
                    id = analysis.Id.ToString(),
                    data = parsed 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing and saving resume");
                var fullMessage = ex.InnerException != null
                    ? $"{ex.Message} → {ex.InnerException.Message}"
                    : ex.Message;
                return StatusCode(500, new { error = "server_error", message = fullMessage });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var studentEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(studentEmail))
                return Unauthorized();

            var history = await _resumeRepository.GetByStudentEmailAsync(studentEmail);
            var result = history.Select(h => new {
                id = h.Id.ToString(),
                fileName = h.FileName,
                analyzedAt = h.AnalyzedAt,
                fullName = h.Data?.FullName
            });

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var analysis = await _resumeRepository.GetByIdAsync(id);
            if (analysis == null)
                return NotFound();

            // Security check: ensure the user owns this analysis
            if (analysis.StudentEmail != User.Identity?.Name)
                return Forbid();

            return Ok(analysis.Data);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var analysis = await _resumeRepository.GetByIdAsync(id);
            if (analysis == null)
                return NotFound();

            if (analysis.StudentEmail != User.Identity?.Name)
                return Forbid();

            await _resumeRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}
