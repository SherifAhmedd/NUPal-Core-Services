using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nupal.Domain.Entities
{
    [BsonIgnoreExtraElements]
    public class ResumeAnalysis
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string StudentEmail { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

        public ResumeData Data { get; set; } = new();
    }
}
