using NUPAL.Core.Application.DTOs;

namespace NUPAL.Core.Application.Interfaces
{
    public interface IResumeParsingService
    {
        Task<ParsedResumeDto> ParseResumeAsync(string resumeText, CancellationToken ct = default);
    }
}
