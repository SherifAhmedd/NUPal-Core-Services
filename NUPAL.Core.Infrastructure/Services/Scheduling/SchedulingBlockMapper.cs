using Nupal.Domain.Entities;
using NUPAL.Core.Application.DTOs;
using NUPAL.Core.Infrastructure.Services.Scheduling.Models;

namespace NUPAL.Core.Infrastructure.Services.Scheduling
{

    internal static class SchedulingBlockMapper
    {

        internal static RawBlockDto ToRawDto(SchedulingBlock b) => new()
        {
            BlockId = b.BlockId,
            Semester = b.Semester,
            Major = b.Major,
            Level = b.Level,
            Courses = b.Courses.Select(c => new RawBlockCourseDto
            {
                CourseName = c.CourseName,
                Section = c.Section,
                Type = c.Type,
                Instructor = c.Instructor,
                Day = c.Day,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                Room = c.Room,
            }).ToList(),
        };

        internal static BlockFeatures ExtractFeatures(RawBlockDto block, List<CourseMapping>? mappings = null)
        {
            var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "", "unknown", "tba", "tbd", "n/a" };

            var courses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var normalizedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var instructors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var days = new HashSet<string>();
            var allSlots = new HashSet<int>();
            var daySlots = new Dictionary<string, HashSet<int>>();
            double totalMins = 0;

            foreach (var c in block.Courses)
            {
                var name = (c.CourseName ?? "").Trim();
                if (!string.IsNullOrEmpty(name)) 
                {
                    courses.Add(name.ToLower());
                    if (mappings != null)
                    {
                        var mapping = mappings.FirstOrDefault(m => 
                            (m.CourseCode != null && m.CourseCode.Equals(name, StringComparison.OrdinalIgnoreCase)) ||
                            m.GetAllNames().Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        
                        if (mapping != null && !string.IsNullOrEmpty(mapping.CourseCode))
                        {
                            normalizedCodes.Add(mapping.CourseCode);
                        }
                    }
                }

                var instr = (c.Instructor ?? "").Trim().ToLower();
                if (!string.IsNullOrEmpty(instr) && !excluded.Contains(instr))
                    instructors.Add(instr);

                var day   = (c.Day ?? "").Trim();
                var slots = SchedulingTimeHelper.TimeSlots(c.StartTime ?? "", c.EndTime ?? "");

                if (!string.IsNullOrEmpty(day))
                {
                    days.Add(day);
                    if (!daySlots.ContainsKey(day)) daySlots[day] = [];
                    foreach (var s in slots) daySlots[day].Add(s);
                }
                var start = SchedulingTimeHelper.ParseTime(c.StartTime ?? "");
                var end   = SchedulingTimeHelper.ParseTime(c.EndTime   ?? "");
                if (start.HasValue && end.HasValue && end > start)
                    totalMins += end.Value - start.Value;
            }

            return new BlockFeatures
            {
                BlockId= block.BlockId,
                Level= block.Level,
                Courses= courses,
                NormalizedCourseCodes= normalizedCodes,
                Instructors= instructors,
                Days= days,
                AllSlots= allSlots,
                NumDays= days.Count,
                TotalHours= Math.Round(totalMins / 60.0 * 10.0) / 10.0,
                DaySlots= daySlots.ToDictionary(kv => kv.Key, kv => kv.Value.OrderBy(x => x).ToList()),
            };
        }


        internal static BlockDto RawToFrontend(RawBlockDto raw, List<CourseMapping>? mappings = null)
        {
            var sessions = raw.Courses
                .Where(c => !string.IsNullOrEmpty(c.StartTime) && !string.IsNullOrEmpty(c.EndTime))
                .Select(c =>
                {
                    var rawName = (c.CourseName ?? "").Trim();
                    string courseId = rawName.Replace(" ", "_").ToUpper()[..Math.Min(12, rawName.Length)];

                    if (mappings != null)
                    {
                        var mapping = mappings.FirstOrDefault(m => 
                            (m.CourseCode != null && m.CourseCode.Equals(rawName, StringComparison.OrdinalIgnoreCase)) ||
                            m.GetAllNames().Any(n => n.Equals(rawName, StringComparison.OrdinalIgnoreCase)));
                        
                        if (mapping != null && !string.IsNullOrEmpty(mapping.CourseCode))
                        {
                            courseId = mapping.CourseCode;
                        }
                    }

                    bool isDoctor = !string.IsNullOrEmpty(c.Section) && c.Section.All(char.IsDigit);
                    string subtype = "Lecture";
                    if (!isDoctor)
                    {
                        subtype = (!string.IsNullOrEmpty(c.Type) && c.Type.Equals("T", StringComparison.OrdinalIgnoreCase)) 
                            ? "Tutorial" 
                            : "Lab";
                    }

                    return new CourseSessionDto
                    {
                        CourseId = courseId,
                        CourseName = rawName,
                        Instructor = string.IsNullOrWhiteSpace(c.Instructor) ? "TBA" : c.Instructor,
                        Day = c.Day ?? "",
                        Start = SchedulingTimeHelper.FormatTime(c.StartTime ?? ""),
                        End = SchedulingTimeHelper.FormatTime(c.EndTime ?? ""),
                        Room = c.Room,
                        Section = c.Section,
                        InstructorType = isDoctor ? "Doctor" : "TA",
                        Subtype = subtype
                    };
                })
                .ToList();

            var baseNamesWithLectures = raw.Courses
                .Where(c => !string.IsNullOrEmpty(c.Type) && (c.Type == "L" || c.Type.StartsWith("L", StringComparison.OrdinalIgnoreCase)))
                .Select(c => c.CourseName.Split('-')[0].Split('(')[0].Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            int lectureCourseCount = baseNamesWithLectures.Count;

            if (lectureCourseCount == 0 && raw.Courses.Count > 0)
            {
                lectureCourseCount = raw.Courses
                    .Select(c => c.CourseName.Split('-')[0].Split('(')[0].Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();
            }

            return new BlockDto
            {
                BlockId = raw.BlockId,
                Semester = raw.Semester,
                Major = raw.Major,
                TotalCredits = lectureCourseCount * 3,
                Courses = sessions,
            };
        }
    }
}
