using Business.Core.ICore;
using Business.Helpers;
using Infrastructure.Repositories.IRepositories;
using iText.Html2pdf;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.Utils;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Pdfa;
using iText.Signatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Core
{
    public class TransformaPdfCore : ITransformaPdfCore
    {
        private const string Intent = "./wwwroot/resources/color/sRGB_CS_profile.icm";

        private readonly IArquivoRepository ArquivoRepository;

        public TransformaPdfCore(IArquivoRepository arquivoRepository)
        {
            ArquivoRepository = arquivoRepository;
        }

        #region Validações

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

        #endregion

        #region Assinatura digital

        public void VerificarAssinaturaDigital(byte[] file)
        {
            Validations.ArquivoValido(file);

            using (MemoryStream readingStream = new MemoryStream(file))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                SignatureUtil signUtil = new SignatureUtil(pdfDocument);
                IList<String> names = signUtil.GetSignatureNames();

                foreach (String name in names)
                {
                    Console.WriteLine("===== " + name + " =====");
                    VerifySignature(signUtil, name);
                }

                pdfDocument.Close();
            }
        }

        public PdfPKCS7 VerifySignature(SignatureUtil signUtil, String name)
        {
            PdfPKCS7 pkcs7 = signUtil.ReadSignatureData(name);

            Console.WriteLine("Signature covers whole document: " + signUtil.SignatureCoversWholeDocument(name));
            Console.WriteLine("Document revision: " + signUtil.GetRevision(name) + " of "
                                  + signUtil.GetTotalRevisions());
            Console.WriteLine("Integrity check OK? " + pkcs7.VerifySignatureIntegrityAndAuthenticity());
            return pkcs7;
        }

        #endregion

        #region Outros

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

        public byte[] MetaPDFA(byte[] file)
        {
            using (MemoryStream readingMemoryStream = new MemoryStream(file))
            using (PdfReader pdfReader = new PdfReader(readingMemoryStream))
            using (PdfDocument readingPdfDocument = new PdfDocument(pdfReader))
            using (MemoryStream writingMemoryStream = new MemoryStream())
            using (PdfWriter pdfWriter = new PdfWriter(writingMemoryStream))
            using (FileStream intentFileStream = new FileStream(Intent, FileMode.Open, FileAccess.Read))
            using (PdfADocument pdfaDocument = new PdfADocument(pdfWriter, PdfAConformanceLevel.PDF_A_1B, new PdfOutputIntent(
                "Custom",
                "",
                "http://www.color.org",
                "sRGB IEC61966-2.1",
                intentFileStream
            )))
            {
                readingPdfDocument.CopyPagesTo(1, readingPdfDocument.GetNumberOfPages(), pdfaDocument);

                readingPdfDocument.Close();
                pdfaDocument.Close();

                return writingMemoryStream.ToArray();
            }
        }
        public byte[] AdicionarMarcaDagua(byte[] file, string text, int angleDegrees = 30, int quantity = 5, float opacity = 0.1f)
        {
            // validações
            Validations.ArquivoValido(file);

            using (MemoryStream readingStream = new MemoryStream(file))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (MemoryStream writingStream = new MemoryStream())
            using (PdfWriter pdfWriter = new PdfWriter(writingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader, pdfWriter))
            using (Document document = new Document(pdfDocument))
            {
                Rectangle pageSize;
                PdfCanvas canvas;
                int n = pdfDocument.GetNumberOfPages();
                float angleRads = (angleDegrees * (float)Math.PI) / 180;
                for (int i = 1; i <= n; i++)
                {
                    PdfPage page = pdfDocument.GetPage(i);
                    pageSize = page.GetPageSize();
                    canvas = new PdfCanvas(page);
                    // Desenhar Marca D'dágua
                    Paragraph p = new Paragraph(text).SetFontSize(60).SetFontColor(ColorConstants.RED);
                    canvas.SaveState();
                    PdfExtGState gs1 = new PdfExtGState().SetFillOpacity(opacity);
                    canvas.SetExtGState(gs1);
                    for (int j = 1; j <= quantity; j++)
                    {
                        document.ShowTextAligned(
                            p,
                            pageSize.GetWidth() / 2,
                            (pageSize.GetHeight() / (quantity + 1)) * j,
                            pdfDocument.GetPageNumber(page),
                            TextAlignment.CENTER, VerticalAlignment.MIDDLE,
                            angleRads
                        );
                    }
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

        //public byte[] OCR(byte[] file)
        //{
        //    var tesseractReader = new Tesseract4LibOcrEngine(tesseract4OcrEngineProperties);
        //    tesseract4OcrEngineProperties.SetPathToTessData(new FileInfo(TESS_DATA_FOLDER));

        //    var ocrPdfCreator = new OcrPdfCreator(tesseractReader);
        //    using (var writer = new PdfWriter(OUTPUT_PDF))
        //    {
        //        ocrPdfCreator.CreatePdf(LIST_IMAGES_OCR, writer).Close();
        //    }
        //}

        //static PdfOutputIntent GetRgbPdfOutputIntent()
        //{
        //    Stream @is = new FileStream(DEFAULT_RGB_COLOR_PROFILE_PATH, FileMode.Open, FileAccess.Read);
        //    return new PdfOutputIntent("", "", "", "sRGB IEC61966-2.1", @is);
        //}

        #endregion

        #region  Auxiliares

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
        
        #endregion
    }
}
