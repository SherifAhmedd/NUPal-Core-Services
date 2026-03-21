using System.IO;

namespace NUPAL.Core.Application.Interfaces
{
    public interface IPdfTextExtractorService
    {
        string ExtractTextFromPdf(Stream pdfStream);
    }
}
