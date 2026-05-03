using Nupal.Domain.Entities;

namespace NUPAL.Core.Application.Interfaces
{
    public interface ISystemSettingsRepository
    {
        Task<SystemSettings> GetSettingsAsync();
        Task UpdateSettingsAsync(SystemSettings settings);
    }
}
