using Business.Core.ICore;
using Infrastructure.Repositories.IRepositories;
using iText.Html2pdf;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Core
{
    public class TransformaPdfCore : ITransformaPdfCore
    {
        private readonly IArquivoRepository ArquivoRepository;

        public TransformaPdfCore(IArquivoRepository arquivoRepository)
        {
            ArquivoRepository = arquivoRepository;
        }

        public byte[] HtmlPdf(byte[] file)
        {
            var html = new MemoryStream(file);
            var output = new MemoryStream();
            HtmlConverter.ConvertToPdf(html, output);
            return output.ToArray();
        }

        public byte[] PdfPagination(byte[] file, int itemsByPage, int page)
        {
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(new MemoryStream(file)));
            ICollection<byte[]> output = new CustomPdfSplitter(pdfDocument).SplitByPageCount(itemsByPage);
            pdfDocument.Close();
            return output.ElementAt(page);
        }

        public byte[] PdfConcatenation(IEnumerable<byte[]> files)
        {
            var outputStream = new MemoryStream();
            var outputDocument = new PdfDocument(new PdfWriter(outputStream));

            foreach (var file in files)
            {
                var document = new PdfDocument(new PdfReader(new MemoryStream(file)));
                document.CopyPagesTo(1, document.GetNumberOfPages(), outputDocument);
                document.Close();
            }

            outputDocument.Close();

            return outputStream.ToArray();
        }

        public async Task<byte[]> PdfConcatenationUsingMinio(IEnumerable<string> files)
        {
            // buscar arquivos no Minio
            var minioFiles = new List<byte[]>();
            foreach (var file in files)
                minioFiles.Add(await ArquivoRepository.GetDocumentoCapturadoAsync(file));

            // concatenar arquivos do Minio
            var outputStream = new MemoryStream();
            var outputDocument = new PdfDocument(new PdfWriter(outputStream));
            foreach (var file in minioFiles)
            {
                var document = new PdfDocument(new PdfReader(new MemoryStream(file)));
                document.CopyPagesTo(1, document.GetNumberOfPages(), outputDocument);
                document.Close();
            }
            outputDocument.Close();

            return outputStream.ToArray();
        }

        public bool IsPdf(byte[] file)
        {
            try
            {
                var reader = new PdfReader(new MemoryStream(file));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsPdfa(byte[] file)
        {
            try
            {
                var reader = new PdfReader(new MemoryStream(file));
                if (reader.GetPdfAConformanceLevel() == null)
                    return false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public byte[] RemoveAnnotations(byte[] file)
        {
            if (!IsPdf(file))
                throw new Exception("Este arquivo não é um documento PDF.");

            var stream = new MemoryStream(file);
            var outputStream = new MemoryStream();
            var pdfDocument = new PdfDocument(new PdfReader(stream), new PdfWriter(outputStream));
            
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                pdfDocument.GetPage(i).GetPdfObject().Remove(PdfName.Annots);

            pdfDocument.Close();

            return outputStream.ToArray();
        }

        private class CustomPdfSplitter : PdfSplitter
        {
            private List<MemoryStream> Destination;
            private int DestionationIndex = 0;

            public CustomPdfSplitter(PdfDocument pdfDocument) : base(pdfDocument)
            {
                Destination = new List<MemoryStream>();
            }

            protected override PdfWriter GetNextPdfWriter(PageRange documentPageRange)
            {
                Destination.Add(new MemoryStream());
                return new PdfWriter(Destination[DestionationIndex++]);
            }

            public ICollection<byte[]> SplitByPageCount(int pageNumber)
            {
                ICollection<byte[]> output = new List<byte[]>();

                var splitDocuments = base.SplitByPageCount(pageNumber);
                foreach (PdfDocument doc in splitDocuments)
                    doc.Close();
                
                foreach (var item in Destination)
                    output.Add(item.ToArray());

                return output;
            }
        }
    }
}
