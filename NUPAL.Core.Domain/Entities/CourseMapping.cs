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
        public List<string> CourseNames { get; set; } = new();
        public int Credits { get; set; }
        public string Category { get; set; } = string.Empty;
        

        
        public IEnumerable<string> GetAllNames()
        {
            return (CourseNames ?? new List<string>()).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct();
        }
    }
}
