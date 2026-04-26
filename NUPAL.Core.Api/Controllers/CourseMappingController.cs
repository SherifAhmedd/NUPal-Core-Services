using Microsoft.AspNetCore.Mvc;
using Nupal.Domain.Entities;
using NUPAL.Core.Application.Interfaces;
using System.Text.Json.Serialization;

namespace NUPAL.Core.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseMappingController : ControllerBase
    {
        private readonly ICourseMappingRepository _repository;

        public CourseMappingController(ICourseMappingRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var mappings = await _repository.GetAllAsync();
            return Ok(mappings);
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> Lookup([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name is required");

            var mapping = await _repository.GetByNameAsync(name);
            if (mapping == null)
            {
                // Try case-insensitive or partial match if needed, but for now exact
                return NotFound($"Course with name '{name}' not found");
            }

            return Ok(mapping);
        }

        [HttpPost("seed")]
        public async Task<IActionResult> Seed([FromBody] Dictionary<string, CourseMappingDto> data)
        {
            if (data == null || data.Count == 0)
                return BadRequest("Data is empty");

            await _repository.DeleteAllAsync();

            var mappings = data.Select(kvp => new CourseMapping
            {
                CourseCode = kvp.Value.Code,
                PolicyName = kvp.Value.PolicyName,
                BlockNames = kvp.Value.BlockNames ?? new(),
                TrackNames = kvp.Value.TrackNames ?? new(),
                AcademicPlanNames = kvp.Value.AcademicPlanNames ?? new()
            }).ToList();

            await _repository.AddRangeAsync(mappings);

            return Ok(new { message = $"Successfully seeded {mappings.Count} course mappings" });
        }
    }

    public class CourseMappingDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("policy_name")]
        public string PolicyName { get; set; } = string.Empty;

        [JsonPropertyName("block_names")]
        public List<string> BlockNames { get; set; } = new();

        [JsonPropertyName("track_names")]
        public List<string> TrackNames { get; set; } = new();

        [JsonPropertyName("academic_plan_names")]
        public List<string> AcademicPlanNames { get; set; } = new();
    }
}
