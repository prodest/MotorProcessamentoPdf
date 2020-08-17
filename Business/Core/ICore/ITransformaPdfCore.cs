namespace Business.Core.ICore
{
    public interface ITransformaPdfCore
    {
        byte[] HtmlPdf(byte[] file);
        byte[] PdfPagination(byte[] file, int itemsByPage, int page);
        bool IsPdf(byte[] file);
        bool IsPdfa(byte[] file);
    }
}
