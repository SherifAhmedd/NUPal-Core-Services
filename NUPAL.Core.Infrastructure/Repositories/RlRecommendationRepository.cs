using MongoDB.Driver;
using MongoDB.Bson;
using Nupal.Domain.Entities;
using NUPAL.Core.Application.Interfaces;

namespace Nupal.Core.Infrastructure.Repositories
{
    public class RlRecommendationRepository : IRlRecommendationRepository
    {
        private readonly IMongoCollection<RlRecommendation> _col;

        public RlRecommendationRepository(IMongoDatabase db)
        {
            _col = db.GetCollection<RlRecommendation>("rl_recommendations");
            
            try
            {
                // Index on StudentId
                var indexKeys = Builders<RlRecommendation>.IndexKeys.Descending(x => x.CreatedAt).Ascending(x => x.StudentId);
                _col.Indexes.CreateOne(new CreateIndexModel<RlRecommendation>(indexKeys));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to create indexes for RlRecommendationRepository: {ex.Message}");
            }
        }

        public async Task CreateAsync(RlRecommendation recommendation)
        {
            await _col.InsertOneAsync(recommendation);
        }

        public async Task<RlRecommendation?> GetByIdAsync(string id)
        {
            return await _col.Find(x => x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
        }

        public async Task<RlRecommendation?> GetLatestByStudentIdAsync(string studentId)
        {
            return await _col.Find(x => x.StudentId == studentId)
                .SortByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
