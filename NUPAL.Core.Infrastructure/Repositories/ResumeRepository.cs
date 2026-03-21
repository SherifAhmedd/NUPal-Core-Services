using MongoDB.Bson;
using MongoDB.Driver;
using Nupal.Domain.Entities;
using NUPAL.Core.Application.Interfaces;

namespace Nupal.Core.Infrastructure.Repositories
{
    public class ResumeRepository : IResumeRepository
    {
        private readonly IMongoCollection<ResumeAnalysis> _col;

        static ResumeRepository()
        {
            if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(ResumeAnalysis)))
            {
                MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<ResumeAnalysis>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }
        }

        public ResumeRepository(IMongoDatabase db)
        {
            _col = db.GetCollection<ResumeAnalysis>("resume_analyses");
            
            // Create Index for StudentEmail for fast history lookup
            var indexKeyDefinition = Builders<ResumeAnalysis>.IndexKeys.Ascending(x => x.StudentEmail);
            var indexModel = new CreateIndexModel<ResumeAnalysis>(indexKeyDefinition);
            _col.Indexes.CreateOne(indexModel);
        }

        public async Task SaveAsync(ResumeAnalysis analysis)
        {
            if (analysis.Id == ObjectId.Empty)
            {
                await _col.InsertOneAsync(analysis);
            }
            else
            {
                await _col.ReplaceOneAsync(x => x.Id == analysis.Id, analysis, new ReplaceOptions { IsUpsert = true });
            }
        }

        public async Task<IEnumerable<ResumeAnalysis>> GetByStudentEmailAsync(string email)
        {
            return await _col.Find(x => x.StudentEmail == email)
                             .SortByDescending(x => x.AnalyzedAt)
                             .ToListAsync();
        }

        public async Task<ResumeAnalysis?> GetByIdAsync(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
                return null;

            return await _col.Find(x => x.Id == objectId).FirstOrDefaultAsync();
        }

        public async Task DeleteAsync(string id)
        {
            if (ObjectId.TryParse(id, out var objectId))
            {
                await _col.DeleteOneAsync(x => x.Id == objectId);
            }
        }
    }
}
