using System.Text.Json.Serialization;

using MongoDB.Bson.Serialization.Attributes;

namespace Nupal.Domain.Entities
{
    [BsonIgnoreExtraElements]
    public class ResumeData
    {
        [JsonPropertyName("firstName")]     public string? FirstName { get; set; }
        [JsonPropertyName("lastName")]      public string? LastName { get; set; }
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
        [JsonPropertyName("experience")]    public List<ResumeExperience> Experience { get; set; } = [];
        [JsonPropertyName("education")]     public List<ResumeEducation> Education { get; set; } = [];
        [JsonPropertyName("projects")]      public List<ResumeProject> Projects { get; set; } = [];
        [JsonPropertyName("certifications")] public List<string> Certifications { get; set; } = [];
        [JsonPropertyName("languages")]     public List<string> Languages { get; set; } = [];
        [JsonPropertyName("awards")]        public List<string> Awards { get; set; } = [];
    }

    [BsonIgnoreExtraElements]
    public class ResumeExperience
    {
        [JsonPropertyName("title")]     public string? Title { get; set; }
        [JsonPropertyName("company")]   public string? Company { get; set; }
        [JsonPropertyName("location")]  public string? Location { get; set; }
        [JsonPropertyName("startDate")] public string? StartDate { get; set; }
        [JsonPropertyName("endDate")]   public string? EndDate { get; set; }
        [JsonPropertyName("isCurrent")] public bool? IsCurrent { get; set; }
        [JsonPropertyName("bullets")]   public List<string> Bullets { get; set; } = [];
    }

    [BsonIgnoreExtraElements]
    public class ResumeEducation
    {
        [JsonPropertyName("degree")]      public string? Degree { get; set; }
        [JsonPropertyName("field")]       public string? Field { get; set; }
        [JsonPropertyName("institution")] public string? Institution { get; set; }
        [JsonPropertyName("location")]    public string? Location { get; set; }
        [JsonPropertyName("startDate")]   public string? StartDate { get; set; }
        [JsonPropertyName("endDate")]     public string? EndDate { get; set; }
        [JsonPropertyName("gpa")]         public string? GPA { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class ResumeProject
    {
        [JsonPropertyName("name")]         public string? Name { get; set; }
        [JsonPropertyName("description")]  public string? Description { get; set; }
        [JsonPropertyName("technologies")] public List<string> Technologies { get; set; } = [];
        [JsonPropertyName("bullets")]      public List<string> Bullets { get; set; } = [];
        [JsonPropertyName("link")]         public string? Link { get; set; }
    }
}
