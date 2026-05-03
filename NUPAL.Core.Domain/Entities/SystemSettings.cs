using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nupal.Domain.Entities
{
    [BsonIgnoreExtraElements]
    public class SystemSettings
    {
        [BsonId]
        public string Id { get; set; } = "Global";
        public string ActiveSemester { get; set; } = "Fall 2025";
    }
}
