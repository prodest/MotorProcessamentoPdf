using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface ITransformaPdfCore
    {
        byte[] PdfConcatenation(IEnumerable<byte[]> files);
        Task<byte[]> PdfConcatenationUsingMinio(IEnumerable<string> files);
        byte[] HtmlPdf(byte[] file);
        byte[] PdfPagination(byte[] file, int itemsByPage, int page);
        bool IsPdf(byte[] file);
        bool IsPdfa1b(byte[] file);
        byte[] RemoveAnnotations(byte[] file);
        byte[] AdicionarMarcaDagua(byte[] file, string text, int angleDegrees = 30, int quantity = 5, float opacity = 0.1f);
        void VerificarAssinaturaDigital(byte[] arquivoByteArray);
        byte[] MetaPDFA(byte[] arquivoBytes);
        void ContemIdentificadorDocumentoEdocs(byte[] arquivoBytes);
    }
}
