using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nupal.Domain.Entities
{
    public class CourseMapping
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string CourseCode { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;
        public List<string> BlockNames { get; set; } = new();
        public List<string> TrackNames { get; set; } = new();
        public List<string> AcademicPlanNames { get; set; } = new();
        
        // Helper to get all names associated with this course
        public IEnumerable<string> GetAllNames()
        {
            var names = new List<string> { PolicyName };
            names.AddRange(BlockNames);
            names.AddRange(TrackNames);
            names.AddRange(AcademicPlanNames);
            return names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct();
        }
    }
}
