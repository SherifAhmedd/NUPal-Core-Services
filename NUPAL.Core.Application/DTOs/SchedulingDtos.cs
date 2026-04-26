using System.Text.Json.Serialization;

namespace NUPAL.Core.Application.DTOs
{
    
    public class RawBlockCourseDto
    {
        [JsonPropertyName("course_name")]
        public string CourseName { get; set; } = "";

        [JsonPropertyName("section")]
        public string? Section { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("instructor")]
        public string? Instructor { get; set; }

        [JsonPropertyName("day")]
        public string? Day { get; set; }

        [JsonPropertyName("start_time")]
        public string? StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public string? EndTime { get; set; }

        [JsonPropertyName("room")]
        public string? Room { get; set; }
    }

    public class RawBlockDto
    {
        [JsonPropertyName("block_id")]
        public string BlockId { get; set; } = "";

        [JsonPropertyName("semester")]
        public string? Semester { get; set; }

        [JsonPropertyName("major")]
        public string? Major { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; } = "";

        [JsonPropertyName("courses")]
        public List<RawBlockCourseDto> Courses { get; set; } = new();
    }


    public class BlockSeedRequestDto
    {
        public List<RawBlockDto> Blocks { get; set; } = new();
    }

    public class SchedulePreferencesDto
    {

        public string Level { get; set; } = "";

        public List<string>? PreferredDays { get; set; }
        public int? NumPreferredDays { get; set; }

        public string DayMode { get; set; } = "count";

        public int MaxDaysPerWeek { get; set; } = 7;

        public double MaxGapHours { get; set; } = 0;

        public string? EarliestTime { get; set; }

        public string? LatestTime { get; set; }

        public List<string>? PreferredInstructors { get; set; }

        public string? ScheduleType { get; set; }
    }

    public class RecommendationRequestDto
    {
        public SchedulePreferencesDto Preferences { get; set; } = new();

        public List<string>? DesiredCourseNames { get; set; }

        public int TopN { get; set; } = 5;

        public bool MatchCoursesOnly { get; set; } = false;
    }

    public class CourseSessionDto
    {
        public string CourseId   { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string Instructor { get; set; } = "";
        public string Day        { get; set; } = "";
        public string Start      { get; set; } = "";
        public string End        { get; set; } = "";
        public string? Room      { get; set; }
        public string? Section   { get; set; }
        public string? Subtype   { get; set; }
        public string? InstructorType { get; set; }
        public string? Color     { get; set; }
    }

    public class BlockDto
    {
        public string BlockId      { get; set; } = "";
        public int    TotalCredits { get; set; }
        public List<CourseSessionDto> Courses { get; set; } = new();
    }

    public class CategorizedInstructorsDto
    {
        public List<string> Doctors { get; set; } = new();
        public List<string> TAs { get; set; } = new();
    }

    public class ScoreBreakdownDto
    {
        public double Similarity  { get; set; }
        public double Coverage    { get; set; }
        public double Compactness { get; set; }
        public double DayBonus    { get; set; }
    }

    public class RecommendationResultDto
    {
        public BlockDto          Block      { get; set; } = new();
        public int               MatchScore { get; set; }
        public List<string>      Reasons    { get; set; } = new();
        public ScoreBreakdownDto Breakdown  { get; set; } = new();
    }
}
