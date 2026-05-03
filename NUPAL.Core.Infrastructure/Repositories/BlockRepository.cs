using MongoDB.Driver;
using Nupal.Domain.Entities;
using NUPAL.Core.Application.Interfaces;

namespace Nupal.Core.Infrastructure.Repositories
{
    public class BlockRepository : IBlockRepository
    {
        private readonly IMongoCollection<SchedulingBlock> _col;

        public BlockRepository(IMongoDatabase db)
        {
            _col = db.GetCollection<SchedulingBlock>("scheduling_blocks");

            try
            {
                try { _col.Indexes.DropOne("block_id_1"); } catch { }
                try { _col.Indexes.DropOne("BlockId_1"); } catch { }

                // Unique compound index on (block_id, semester)
                var keys = Builders<SchedulingBlock>.IndexKeys
                    .Ascending(x => x.BlockId)
                    .Ascending(x => x.Semester);
                var opts = new CreateIndexOptions { Unique = true };
                _col.Indexes.CreateOne(new CreateIndexModel<SchedulingBlock>(keys, opts));

                // Index to speed up level queries
                var levelKeys = Builders<SchedulingBlock>.IndexKeys.Ascending(x => x.Level);
                _col.Indexes.CreateOne(new CreateIndexModel<SchedulingBlock>(levelKeys));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] BlockRepository index creation: {ex.Message}");
            }
        }

        public async Task<List<SchedulingBlock>> GetAllAsync(string? level = null, string? semester = null)
        {
            var filters = new List<FilterDefinition<SchedulingBlock>>();

            if (!string.IsNullOrWhiteSpace(level))
            {
                filters.Add(Builders<SchedulingBlock>.Filter.Regex(
                    x => x.Level,
                    new MongoDB.Bson.BsonRegularExpression($"^{level.Trim()}$", "i")));
            }

            if (!string.IsNullOrWhiteSpace(semester))
            {
                filters.Add(Builders<SchedulingBlock>.Filter.Regex(
                    x => x.Semester,
                    new MongoDB.Bson.BsonRegularExpression($"^{semester.Trim()}$", "i")));
            }

            var filter = filters.Count > 0 
                ? Builders<SchedulingBlock>.Filter.And(filters)
                : Builders<SchedulingBlock>.Filter.Empty;

            return await _col.Find(filter)
                             .SortBy(x => x.BlockId)
                             .ToListAsync();
        }

        public async Task<SchedulingBlock?> GetByBlockIdAsync(string blockId, string? semester)
        {
            var filter = Builders<SchedulingBlock>.Filter.Regex(
                x => x.BlockId,
                new MongoDB.Bson.BsonRegularExpression($"^{blockId.Trim()}$", "i"));
                
            if (!string.IsNullOrEmpty(semester))
            {
                filter &= Builders<SchedulingBlock>.Filter.Regex(
                    x => x.Semester,
                    new MongoDB.Bson.BsonRegularExpression($"^{semester.Trim()}$", "i"));
            }

            return await _col.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<int> UpsertManyAsync(IEnumerable<SchedulingBlock> blocks)
        {
            var blockList = blocks.ToList();
            if (blockList.Count == 0) return 0;

            var writes = blockList.Select(block =>
            {
                var filter = Builders<SchedulingBlock>.Filter.And(
                    Builders<SchedulingBlock>.Filter.Eq(x => x.BlockId, block.BlockId),
                    Builders<SchedulingBlock>.Filter.Eq(x => x.Semester, block.Semester)
                );
                return new ReplaceOneModel<SchedulingBlock>(filter, block) { IsUpsert = true };
            });

            var result = await _col.BulkWriteAsync(writes);
            return (int)(result.InsertedCount + result.ModifiedCount + result.Upserts.Count);
        }

        public async Task CreateAsync(SchedulingBlock block)
        {
            await _col.InsertOneAsync(block);
        }

        public async Task UpdateAsync(SchedulingBlock block)
        {
            var filter = Builders<SchedulingBlock>.Filter.And(
                Builders<SchedulingBlock>.Filter.Eq(x => x.BlockId, block.BlockId),
                Builders<SchedulingBlock>.Filter.Eq(x => x.Semester, block.Semester)
            );
            await _col.ReplaceOneAsync(filter, block);
        }

        public async Task DeleteByBlockIdAsync(string blockId, string? semester)
        {
            var filter = Builders<SchedulingBlock>.Filter.Eq(x => x.BlockId, blockId);
            if (!string.IsNullOrEmpty(semester))
            {
                filter &= Builders<SchedulingBlock>.Filter.Eq(x => x.Semester, semester);
            }
            await _col.DeleteManyAsync(filter);
        }
    }
}
