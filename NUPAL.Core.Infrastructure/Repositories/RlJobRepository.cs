using MongoDB.Driver;
using MongoDB.Bson;
using Nupal.Domain.Entities;
using NUPAL.Core.Application.Interfaces;

namespace Nupal.Core.Infrastructure.Repositories
{
    public class RlJobRepository : IRlJobRepository
    {
        private readonly IMongoCollection<RlJob> _col;

        public RlJobRepository(IMongoDatabase db)
        {
            _col = db.GetCollection<RlJob>("rl_jobs");
            
            try
            {
                // Index on StudentId and CreatedAt for fast lookups
                var indexKeys = Builders<RlJob>.IndexKeys.Descending(x => x.CreatedAt).Ascending(x => x.StudentId);
                _col.Indexes.CreateOne(new CreateIndexModel<RlJob>(indexKeys));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to create indexes for RlJobRepository: {ex.Message}");
            }
        }

        public async Task CreateAsync(RlJob job)
        {
            await _col.InsertOneAsync(job);
        }

        public async Task UpdateStatusAsync(string jobId, JobStatus status, string? error = null)
        {
            var filter = Builders<RlJob>.Filter.Eq(x => x.Id, ObjectId.Parse(jobId));
            
            var update = Builders<RlJob>.Update
                .Set(x => x.Status, status);

            if (status == JobStatus.Running)
                update = update.Set(x => x.StartedAt, DateTime.UtcNow);
            
            if (status == JobStatus.Ready || status == JobStatus.Failed)
                update = update.Set(x => x.FinishedAt, DateTime.UtcNow);

            if (error != null)
                update = update.Set(x => x.Error, error);

            await _col.UpdateOneAsync(filter, update);
        }

        public async Task UpdateResultAsync(string jobId, string recommendationId)
        {
            var filter = Builders<RlJob>.Filter.Eq(x => x.Id, ObjectId.Parse(jobId));
            var update = Builders<RlJob>.Update
                .Set(x => x.ResultRecommendationId, recommendationId)
                .Set(x => x.Status, JobStatus.Ready)
                .Set(x => x.FinishedAt, DateTime.UtcNow);

            await _col.UpdateOneAsync(filter, update);
        }

        public async Task<RlJob?> GetByIdAsync(string id)
        {
            return await _col.Find(x => x.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
        }

        public async Task<RlJob?> GetLatestByStudentIdAsync(string studentId)
        {
            return await _col.Find(x => x.StudentId == studentId)
                .SortByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<RlJob>> GetActiveJobsAsync()
        {
            // Debugging: Return ALL jobs to see failed/finished ones too
            // var filter = Builders<RlJob>.Filter.In(x => x.Status, new[] { JobStatus.Queued, JobStatus.Running });
            var filter = Builders<RlJob>.Filter.Empty;
            return await _col.Find(filter).SortByDescending(x => x.CreatedAt).Limit(10).ToListAsync();
        }
    }
}
