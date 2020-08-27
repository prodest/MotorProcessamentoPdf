using Business.Core.ICore;
using Business.Helpers;
using Infrastructure.Repositories.IRepositories;
using iText.Html2pdf;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.Utils;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
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

        public byte[] AdicionarMarcaDagua(byte[] file, string text)
        {
            // validações
            Validations.ArquivoValido(file);

            using (MemoryStream readingStream = new MemoryStream(file))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (MemoryStream writingStream = new MemoryStream())
            using (PdfWriter pdfWriter = new PdfWriter(writingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader, pdfWriter))
            {
                Document document = new Document(pdfDocument);
                Rectangle pageSize;
                PdfCanvas canvas;
                int n = pdfDocument.GetNumberOfPages();
                for (int i = 1; i <= n; i++) {
                    PdfPage page = pdfDocument.GetPage(i);
                    pageSize = page.GetPageSize();
                    canvas = new PdfCanvas(page);
                    //Draw watermark
                    Paragraph p = new Paragraph(text).SetFontSize(60);
                    canvas.SaveState();
                    PdfExtGState gs1 = new PdfExtGState().SetFillOpacity(0.2f);
                    canvas.SetExtGState(gs1);
                    document.ShowTextAligned(
                        p,
                        pageSize.GetWidth() / 2, pageSize.GetHeight() / 2,
                        pdfDocument.GetPageNumber(page),
                        TextAlignment.CENTER, 
                        VerticalAlignment.MIDDLE, 
                        45);
                    canvas.RestoreState();
                }
                pdfDocument.Close();

                return writingStream.ToArray();
            }

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
            Validations.IsPdf(file);
            return true;
        }

        public bool IsPdfa1b(byte[] file)
        {
            Validations.IsPdfa1b(file);
            return true;
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
