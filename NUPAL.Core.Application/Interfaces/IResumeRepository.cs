using Nupal.Domain.Entities;

namespace NUPAL.Core.Application.Interfaces
{
    public interface IResumeRepository
    {
        Task SaveAsync(ResumeAnalysis analysis);
        Task<IEnumerable<ResumeAnalysis>> GetByStudentEmailAsync(string email);
        Task<ResumeAnalysis?> GetByIdAsync(string id);
        Task DeleteAsync(string id);
    }
}
