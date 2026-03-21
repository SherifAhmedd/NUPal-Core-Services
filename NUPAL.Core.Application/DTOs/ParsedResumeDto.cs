using System.Text.Json.Serialization;

namespace NUPAL.Core.Application.DTOs
{
    public class ParsedResumeDto
    {
        [JsonPropertyName("fullName")]      public string? FullName { get; set; }
        [JsonPropertyName("email")]         public string? Email { get; set; }
        [JsonPropertyName("phone")]         public string? Phone { get; set; }
        [JsonPropertyName("location")]      public string? Location { get; set; }
        [JsonPropertyName("linkedIn")]      public string? LinkedIn { get; set; }
        [JsonPropertyName("gitHub")]        public string? GitHub { get; set; }
        [JsonPropertyName("website")]       public string? Website { get; set; }
        [JsonPropertyName("summary")]       public string? Summary { get; set; }
        [JsonPropertyName("technicalSkills")] public List<string> TechnicalSkills { get; set; } = [];
        [JsonPropertyName("softSkills")]    public List<string> SoftSkills { get; set; } = [];
        [JsonPropertyName("experience")]    public List<ResumeExperienceDto> Experience { get; set; } = [];
        [JsonPropertyName("education")]     public List<ResumeEducationDto> Education { get; set; } = [];
        [JsonPropertyName("projects")]      public List<ResumeProjectDto> Projects { get; set; } = [];
        [JsonPropertyName("certifications")] public List<string> Certifications { get; set; } = [];
        [JsonPropertyName("languages")]     public List<string> Languages { get; set; } = [];
        [JsonPropertyName("awards")]        public List<string> Awards { get; set; } = [];
    }

    public class ResumeExperienceDto
    {
        [JsonPropertyName("title")]     public string? Title { get; set; }
        [JsonPropertyName("company")]   public string? Company { get; set; }
        [JsonPropertyName("location")]  public string? Location { get; set; }
        [JsonPropertyName("startDate")] public string? StartDate { get; set; }
        [JsonPropertyName("endDate")]   public string? EndDate { get; set; }
        [JsonPropertyName("isCurrent")] public bool? IsCurrent { get; set; }
        [JsonPropertyName("bullets")]   public List<string> Bullets { get; set; } = [];
    }

    public class ResumeEducationDto
    {
        [JsonPropertyName("degree")]      public string? Degree { get; set; }
        [JsonPropertyName("field")]       public string? Field { get; set; }
        [JsonPropertyName("institution")] public string? Institution { get; set; }
        [JsonPropertyName("location")]    public string? Location { get; set; }
        [JsonPropertyName("startDate")]   public string? StartDate { get; set; }
        [JsonPropertyName("endDate")]     public string? EndDate { get; set; }
        [JsonPropertyName("gpa")]         public string? GPA { get; set; }
    }

    public class ResumeProjectDto
    {
        [JsonPropertyName("name")]         public string? Name { get; set; }
        [JsonPropertyName("description")]  public string? Description { get; set; }
        [JsonPropertyName("technologies")] public List<string> Technologies { get; set; } = [];
        [JsonPropertyName("link")]         public string? Link { get; set; }
    }
}
