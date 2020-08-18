using System.Collections.Generic;

namespace Business.Core.ICore
{
    public interface ITransformaPdfCore
    {
        byte[] PdfConcatenation(ICollection<byte[]> files);
        byte[] HtmlPdf(byte[] file);
        byte[] PdfPagination(byte[] file, int itemsByPage, int page);
        bool IsPdf(byte[] file);
        bool IsPdfa(byte[] file);
        byte[] RemoveAnnotations(byte[] file);
    }
}
