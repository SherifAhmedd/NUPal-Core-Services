using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nupal.Domain.Entities
{
    [BsonIgnoreExtraElements]
    public class JobFitResult
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string StudentEmail { get; set; } = string.Empty;
        public string JobUrl { get; set; } = string.Empty;
        public string JobText { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

        // Snapshot of the full analysis as JSON to avoid circular dependency
        public string AnalysisJson { get; set; } = string.Empty;
    }
}
