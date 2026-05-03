using NUPAL.Core.Application.DTOs;

namespace NUPAL.Core.Application.Interfaces
{
    public interface IAiService
    {
        Task<BlockDto> ParseScheduleTextAsync(string rawText);
        Task<BlockDto> ParseSchedulePdfAsync(Stream pdfStream);
    }
}
