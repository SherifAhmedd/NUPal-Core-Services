using Nupal.Domain.Entities;

namespace NUPAL.Core.Application.Interfaces
{
    public interface ICourseMappingRepository
    {
        Task<CourseMapping?> GetByCodeAsync(string code);
        Task<CourseMapping?> GetByNameAsync(string name);
        Task<List<CourseMapping>> GetAllAsync();
        Task AddAsync(CourseMapping mapping);
        Task AddRangeAsync(IEnumerable<CourseMapping> mappings);
        Task UpdateAsync(CourseMapping mapping);
        Task DeleteAllAsync();
    }
}
