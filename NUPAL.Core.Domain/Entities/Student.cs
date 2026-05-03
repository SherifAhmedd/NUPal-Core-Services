using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Nupal.Domain.Entities
{
    [BsonIgnoreExtraElements]
    public class Account
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } = "student"; // "student" | "admin"
    }

    [BsonIgnoreExtraElements]
    public class Education
    {
        public double TotalCredits { get; set; }
        public int NumSemesters { get; set; }
        public List<Semester> Semesters { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Student
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public Account Account { get; set; }
        public Education Education { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string? LatestRecommendationId { get; set; }
    }
}
