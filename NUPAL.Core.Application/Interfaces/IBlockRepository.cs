using Nupal.Domain.Entities;

namespace NUPAL.Core.Application.Interfaces
{
    public interface IBlockRepository
    {
        Task<List<SchedulingBlock>> GetAllAsync(string? level = null, string? semester = null);

        Task<SchedulingBlock?> GetByBlockIdAsync(string blockId, string? semester);

        Task<int> UpsertManyAsync(IEnumerable<SchedulingBlock> blocks);
        Task CreateAsync(SchedulingBlock block);
        Task UpdateAsync(SchedulingBlock block);
        Task DeleteByBlockIdAsync(string blockId, string? semester);
    }
}
