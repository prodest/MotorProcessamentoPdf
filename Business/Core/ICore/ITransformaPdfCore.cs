using Business.Shared;
using Business.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Business.Core.ICore
{
    public interface ITransformaPdfCore
    {
        //Task<byte[]> Merge(IEnumerable<MergeItem> items);
        byte[] PdfConcatenation(IEnumerable<byte[]> files);
        Task<byte[]> PdfConcatenation(IEnumerable<string> urls);
        byte[] HtmlPdf(string html);
        byte[] HtmlPdf(byte[] file);
        byte[] PdfPagination(byte[] file, int itemsByPage, int page);
        bool IsPdf(byte[] file);
        bool IsPdfa1b(byte[] file);
        byte[] RemoveAnnotations(byte[] file);
        byte[] MetaPDFA(byte[] file);
        bool ValidarRestricoesLeituraOuAltaretacao(byte[] file);
        ApiResponse<PdfInfo> PdfInfo(byte[] file);
        Task<ApiResponse<PdfInfo>> PdfInfo(string url);
    }
}
