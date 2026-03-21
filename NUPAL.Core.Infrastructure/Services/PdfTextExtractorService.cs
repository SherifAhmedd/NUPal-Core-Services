using System.IO;
using System.Text;
using UglyToad.PdfPig;
using NUPAL.Core.Application.Interfaces;

namespace NUPAL.Core.Infrastructure.Services
{
    public class PdfTextExtractorService : IPdfTextExtractorService
    {
        public string ExtractTextFromPdf(Stream pdfStream)
        {
            using var ms = new MemoryStream();
            pdfStream.CopyTo(ms);
            ms.Position = 0;

            var sb = new StringBuilder();
            using var pdfDoc = PdfDocument.Open(ms.ToArray());
            foreach (var page in pdfDoc.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }
    }
}
