namespace Business.Core.ICore
{
    public interface ITransformaPdfCore
    {
        byte[] HtmlPdf(byte[] file);
        byte[] PdfPagination(byte[] file, int itemsByPage, int page);
    }
}
