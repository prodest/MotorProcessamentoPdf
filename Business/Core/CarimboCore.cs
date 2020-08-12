using Business.Core.ICore;
using iText.IO.Source;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Business.Core
{
    public class CarimboCore : ICarimboCore
    {
        public static readonly String DEST = @"C:\Users\prodest1\Desktop\splitDocument1_{0}.pdf";
        public static readonly String RESOURCE = @"C:\Users\prodest1\Desktop\landslides.pdf";

        public async Task Teste()
        {
            var response = await PostAndDownloadArrayAsync(@"http://localhost:53249/landslides.pdf");
            CarimboLateralArray("XXXX-XXXXXX", "Documento Original", DateTime.Now.ToString("dd/MM/yyyy"), response);

            //var bytesFromFile = File.ReadAllBytes(RESOURCE);
            //CarimboLateralArray("XXXX-XXXXXX", "Documento Original", DateTime.Now.ToString("dd/MM/yyyy"), bytesFromFile);

            //CarimboLateralFileSystem("XXXX-XXXXXX", "Documento Original", DateTime.Now.ToString("dd/MM/yyyy"));

            //await CarimboLateralStream();
        }

        #region carimbos

        private async Task CarimboLateralStream()
        {
            Stream result;
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(@"http://localhost:53249/landslides.pdf"))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new Exception(await response.Content.ReadAsStringAsync());

                    result = await response.Content.ReadAsStreamAsync();

                    CarimboLateralStream("XXXX-XXXXXX", "Documento Original", DateTime.Now.ToString("dd/MM/yyyy"), result);
                }
            }
        }

        public void CarimboLateralFileSystem(string codigoDocumento, string tipoDocumento, string dataHora)
        {
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(RESOURCE), new PdfWriter($"wwwroot/{Guid.NewGuid()}.pdf"));

            Document doc = new Document(pdfDocument);

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var carimboLateral = CarimboLateralDocumento(
                    codigoDocumento,
                    tipoDocumento,
                    dataHora,
                    i,
                    pdfDocument.GetNumberOfPages()
                );

                Rectangle pageSize = pdfDocument.GetPage(i).GetPageSize();
                doc.ShowTextAligned(
                    carimboLateral,
                    pageSize.GetWidth(),
                    pageSize.GetHeight() / 2,
                    i,
                    TextAlignment.CENTER, VerticalAlignment.BOTTOM,
                    0.5f * (float)Math.PI
                );
            }

            doc.Close();
        }

        public void CarimboLateralArray(string codigoDocumento, string tipoDocumento, string dataHora, byte[] arquivo)
        {
            PdfDocument pdfDocument = null;
            try
            {
                var reader = new PdfReader(new MemoryStream(arquivo));
                var writer = new PdfWriter($"wwwroot/{Guid.NewGuid()}.pdf");
                pdfDocument = new PdfDocument(reader, writer);
            }
            catch (Exception e)
            {
                throw;
            }

            Document doc = new Document(pdfDocument);

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var carimboLateral = CarimboLateralDocumento(
                    codigoDocumento,
                    tipoDocumento,
                    dataHora,
                    i,
                    pdfDocument.GetNumberOfPages()
                );

                Rectangle pageSize = pdfDocument.GetPage(i).GetPageSize();
                doc.ShowTextAligned(
                    carimboLateral,
                    pageSize.GetWidth(),
                    pageSize.GetHeight() / 2,
                    i,
                    TextAlignment.CENTER, VerticalAlignment.BOTTOM,
                    0.5f * (float)Math.PI
                );

            }

            doc.Close();
        }

        public void CarimboLateralStream(string codigoDocumento, string tipoDocumento, string dataHora, Stream arquivo)
        {
            PdfDocument pdfDocument = null;
            try
            {
                var reader = new PdfReader(arquivo);
                var writer = new PdfWriter($"wwwroot/{Guid.NewGuid()}.pdf");
                pdfDocument = new PdfDocument(reader, writer);
            }
            catch (Exception e)
            {
                throw;
            }

            Document doc = new Document(pdfDocument);

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var carimboLateral = CarimboLateralDocumento(
                    codigoDocumento,
                    tipoDocumento,
                    dataHora,
                    i,
                    pdfDocument.GetNumberOfPages()
                );

                Rectangle pageSize = pdfDocument.GetPage(i).GetPageSize();
                doc.ShowTextAligned(
                    carimboLateral,
                    pageSize.GetWidth(),
                    pageSize.GetHeight() / 2,
                    i,
                    TextAlignment.CENTER, VerticalAlignment.BOTTOM,
                    0.5f * (float)Math.PI
                );

            }

            doc.Close();
        }

        #endregion

        public void SeccionarPDF()
        {
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(RESOURCE));
            IList<PdfDocument> splitDocuments = new CustomPdfSplitter(pdfDocument, DEST).SplitByPageCount(2);
            foreach (PdfDocument doc in splitDocuments)
                doc.Close();
            pdfDocument.Close();
        }

        public byte[] Copia(byte[] arquivo, string codigoProcesso, string geradoPor, string dataHora, int paginaInicial = 1)
        {
            ByteArrayOutputStream baos = new ByteArrayOutputStream();
            PdfDocument pdfDocument;
            try
            {
                pdfDocument = new PdfDocument(
                    new PdfReader(new MemoryStream(arquivo)), 
                    new PdfWriter(baos)
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

            paginaInicial--;
            int paginaFinal = paginaInicial + pdfDocument.GetNumberOfPages();
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                PdfPage page = pdfDocument.GetPage(i);
                Rectangle rectangle = new Rectangle(0, 0, 10, page.GetPageSize().GetHeight());
                Canvas canvas = new Canvas(page, rectangle);

                var paragraph = CarimboDeCopia(
                    codigoProcesso,
                    geradoPor,
                    dataHora,
                    paginaInicial + i,
                    paginaFinal
                );

                canvas.ShowTextAligned(
                    paragraph,
                    0, page.GetPageSize().GetHeight() / 2,
                    i,
                    TextAlignment.CENTER, VerticalAlignment.TOP,
                    0.5f * (float)Math.PI
                );

                canvas.Close();
            }

            pdfDocument.Close();

            return baos.ToArray();
        }

        #region Auxiliares

        private async Task<Stream> PostAndDownloadStreamAsync(string url)
        {
            Stream result;
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new Exception(await response.Content.ReadAsStringAsync());

                    result = await response.Content.ReadAsStreamAsync();
                }
            }

            return result;
        }

        private async Task<byte[]> PostAndDownloadArrayAsync(string url)
        {
            byte[] result;
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new Exception(await response.Content.ReadAsStringAsync());

                    result = await response.Content.ReadAsByteArrayAsync();
                }
            }

            return result;
        }

        private class CustomPdfSplitter : PdfSplitter
        {
            private String dest;
            private int partNumber = 1;

            public CustomPdfSplitter(PdfDocument pdfDocument, String dest) : base(pdfDocument)
            {
                this.dest = dest;
            }

            protected override PdfWriter GetNextPdfWriter(PageRange documentPageRange)
            {
                return new PdfWriter(String.Format(dest, partNumber++));
            }
        }

        private Paragraph CarimboLateralDocumento(string codigoDocumento, string tipoDocumento, string dataHora, int paginaInicial, int paginaFinal)
        {
            var text = new Text($"{codigoDocumento.ToUpper()} - E-DOCS - {tipoDocumento.ToUpper()} {dataHora} PÁGINA {paginaInicial} / {paginaFinal}");

            var style = new Style();
            style.SetFontColor(ColorConstants.BLUE);
            style.SetBackgroundColor(ColorConstants.GRAY);
            style.SetPaddingBottom(5);
            text.AddStyle(style);

            var paragraph = new Paragraph(text);
            return paragraph;
        }

        private Paragraph CarimboDeCopia(string codigoProcesso, string geradoPor, string dataHora, int paginaInicial, int paginaFinal)
        {
            var text = new Text($"E-DOCS - CÓPIA DO PROCESSO {codigoProcesso.ToUpper()} GERADO POR {geradoPor.ToUpper()} EM {dataHora} PÁGINA {paginaInicial} / {paginaFinal}");

            var style = new Style();
            style.SetFontColor(ColorConstants.RED);
            style.SetBackgroundColor(ColorConstants.GRAY);
            style.SetPaddingTop(5);
            text.AddStyle(style);

            var paragraph = new Paragraph(text);
            return paragraph;
        }

        #endregion

    }
}
