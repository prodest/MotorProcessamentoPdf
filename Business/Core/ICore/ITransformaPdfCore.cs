using Business.Shared.Models;
using System.Collections.Generic;

namespace Business.Core.ICore
{
    public interface ITransformaPdfCore
    {
        byte[] PdfConcatenation(IEnumerable<byte[]> files);
        byte[] HtmlPdf(byte[] file);
        byte[] PdfPagination(byte[] file, int itemsByPage, int page);
        bool IsPdf(byte[] file);
        bool IsPdfa1b(byte[] file);
        byte[] RemoveAnnotations(byte[] file);
        byte[] MetaPDFA(byte[] file);
        bool ValidarRestricoesLeituraOuAltaretacao(byte[] file);
        ApiResponse<PdfInfo> PdfInfo(byte[] arquivoByteArray);
    }
}
