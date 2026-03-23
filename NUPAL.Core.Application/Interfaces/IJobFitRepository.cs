using Nupal.Domain.Entities;

namespace NUPAL.Core.Application.Interfaces
{
    public interface IJobFitRepository
    {
        Task SaveAsync(JobFitResult result);
        Task<IEnumerable<JobFitResult>> GetByStudentEmailAsync(string email);
        Task<JobFitResult?> GetByIdAsync(string id);
        Task DeleteAsync(string id);
    }
}
