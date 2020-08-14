namespace Business.Core.ICore
{
    public interface ITransformaPdfCore
    {
        byte[] PdfPagination(byte[] file, int itemsByPage, int page);
    }
}
