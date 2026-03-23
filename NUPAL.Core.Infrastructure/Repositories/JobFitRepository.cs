using MongoDB.Bson;
using MongoDB.Driver;
using Nupal.Domain.Entities;
using NUPAL.Core.Application.Interfaces;

namespace Nupal.Core.Infrastructure.Repositories
{
    public class JobFitRepository : IJobFitRepository
    {
        private readonly IMongoCollection<JobFitResult> _col;

        public JobFitRepository(IMongoDatabase db)
        {
            _col = db.GetCollection<JobFitResult>("job_fit_results");

            // Index on StudentEmail for fast history lookup
            var indexKey = Builders<JobFitResult>.IndexKeys.Ascending(x => x.StudentEmail);
            _col.Indexes.CreateOne(new CreateIndexModel<JobFitResult>(indexKey));
        }

        public async Task SaveAsync(JobFitResult result)
        {
            if (result.Id == ObjectId.Empty)
                await _col.InsertOneAsync(result);
            else
                await _col.ReplaceOneAsync(x => x.Id == result.Id, result, new ReplaceOptions { IsUpsert = true });
        }

        public async Task<IEnumerable<JobFitResult>> GetByStudentEmailAsync(string email)
        {
            return await _col.Find(x => x.StudentEmail == email)
                             .SortByDescending(x => x.AnalyzedAt)
                             .ToListAsync();
        }

        public async Task<JobFitResult?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId)) return null;
            return await _col.Find(x => x.Id == objectId).FirstOrDefaultAsync();
        }

        public async Task DeleteAsync(string id)
        {
            if (ObjectId.TryParse(id, out var objectId))
                await _col.DeleteOneAsync(x => x.Id == objectId);
        }
    }
}
