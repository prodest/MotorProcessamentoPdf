using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface ITransformaPdfCore
    {
        byte[] PdfConcatenation(ICollection<byte[]> files);
        Task<byte[]> PdfConcatenationUsingMinio(ICollection<string> files);
        byte[] HtmlPdf(byte[] file);
        byte[] PdfPagination(byte[] file, int itemsByPage, int page);
        bool IsPdf(byte[] file);
        bool IsPdfa(byte[] file);
        byte[] RemoveAnnotations(byte[] file);
    }
}
