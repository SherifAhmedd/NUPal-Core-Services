namespace NUPAL.Core.Application.Interfaces
{
    public interface ICourseNormalizationService
    {
        Task<string> NormalizeToCodeAsync(string courseName);
        Task<List<string>> NormalizeToCodesAsync(IEnumerable<string> courseNames);
    }
}
