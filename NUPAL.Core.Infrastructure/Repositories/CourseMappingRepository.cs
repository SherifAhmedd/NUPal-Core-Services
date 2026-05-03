using MongoDB.Driver;
using Nupal.Domain.Entities;
using NUPAL.Core.Application.Interfaces;

namespace Nupal.Core.Infrastructure.Repositories
{
    public class CourseMappingRepository : ICourseMappingRepository
    {
        private readonly IMongoCollection<CourseMapping> _col;

        static CourseMappingRepository()
        {
            if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(CourseMapping)))
            {
                MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<CourseMapping>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }
        }

        public CourseMappingRepository(IMongoDatabase db)
        {
            _col = db.GetCollection<CourseMapping>("course_mappings");
            try
            {
                var idxs = new[]
                {
                    new CreateIndexModel<CourseMapping>(Builders<CourseMapping>.IndexKeys.Ascending(x => x.CourseCode), new CreateIndexOptions { Unique = true }),
                };
                _col.Indexes.CreateMany(idxs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to create indexes for CourseMappingRepository: {ex.Message}");
            }
        }

        public async Task<CourseMapping?> GetByCodeAsync(string code)
        {
            return await _col.Find(x => x.CourseCode.ToLower() == code.ToLower()).FirstOrDefaultAsync();
        }

        public async Task<CourseMapping?> GetByNameAsync(string name)
        {
            var lowerName = name.ToLower();
            
            // Note: For small collections, ToListAsync and LINQ is cleaner and more predictable 
            // than complex MongoDB case-insensitive array queries without a proper text index.
            var all = await _col.Find(_ => true).ToListAsync();
            
            return all.FirstOrDefault(x => 
                x.CourseNames != null && x.CourseNames.Any(n => n.ToLower() == lowerName)
            );
        }

        public async Task<List<CourseMapping>> GetAllAsync()
        {
            return await _col.Find(_ => true).ToListAsync();
        }

        public async Task AddAsync(CourseMapping mapping)
        {
            await _col.InsertOneAsync(mapping);
        }

        public async Task AddRangeAsync(IEnumerable<CourseMapping> mappings)
        {
            await _col.InsertManyAsync(mappings);
        }

        public async Task UpdateAsync(CourseMapping mapping)
        {
            await _col.ReplaceOneAsync(x => x.Id == mapping.Id, mapping);
        }

        public async Task DeleteAllAsync()
        {
            await _col.DeleteManyAsync(_ => true);
        }
    }
}
