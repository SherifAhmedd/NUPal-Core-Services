using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using NUPAL.Core.Application.Interfaces;

namespace NUPAL.Core.API.Controllers
{
    [ApiController]
    [Route("api/resume")]
    [Authorize]
    public class ResumeParserController : ControllerBase
    {
        private readonly IPdfTextExtractorService _pdfTextExtractorService;
        private readonly IResumeParsingService _resumeParsingService;
        private readonly ILogger<ResumeParserController> _logger;

        public ResumeParserController(
            IPdfTextExtractorService pdfTextExtractorService,
            IResumeParsingService resumeParsingService,
            ILogger<ResumeParserController> logger)
        {
            _pdfTextExtractorService = pdfTextExtractorService;
            _resumeParsingService = resumeParsingService;
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
                return Ok(parsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing resume");
                var fullMessage = ex.InnerException != null
                    ? $"{ex.Message} → {ex.InnerException.Message}"
                    : ex.Message;
                return StatusCode(500, new { error = "server_error", message = fullMessage });
            }
        }
    }
}
