using Business.Core.ICore;
using iText.IO.Font.Constants;
using iText.IO.Source;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.IO;

namespace Business.Core
{
    public class CarimboCore : ICarimboCore
    {
        #region carimbos

        public byte[] ValorLegal(byte[] arquivo, string registro, string valorLegal, string dataHora)
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

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                PdfPage page = pdfDocument.GetPage(i);
                Rectangle rectangle = new Rectangle(0, 0, 10, page.GetPageSize().GetHeight());
                Canvas canvas = new Canvas(page, rectangle);

                var paragraph = ValorLegalParagrafo(
                    registro,
                    valorLegal,
                    dataHora,
                    i,
                    pdfDocument.GetNumberOfPages()
                );

                Rectangle pageSize = pdfDocument.GetPage(i).GetPageSize();
                canvas.ShowTextAligned(
                    paragraph,
                    pageSize.GetWidth(),
                    pageSize.GetHeight() / 2,
                    i,
                    TextAlignment.CENTER, VerticalAlignment.BOTTOM,
                    0.5f * (float)Math.PI
                );

                canvas.Close();
            }

            pdfDocument.Close();

            return baos.ToArray();
        }

        public byte[] CopiaProcesso(byte[] arquivo, string protocolo, string geradoPor, string dataHora, int totalPaginas, int paginaInicial = 1)
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
            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                PdfPage page = pdfDocument.GetPage(i);
                Rectangle rectangle = new Rectangle(0, 0, 10, page.GetPageSize().GetHeight());
                Canvas canvas = new Canvas(page, rectangle);

                var paragraph = CopiaProcessoParagrafo(
                    protocolo,
                    geradoPor,
                    dataHora,
                    paginaInicial + i,
                    totalPaginas
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

        #endregion

        #region Auxiliares

        private Paragraph ValorLegalParagrafo(string protocolo, string valorLegal, string dataHora, int paginaInicial, int paginaFinal)
        {
            var text = new Text($"{protocolo.ToUpper()} - E-DOCS - {valorLegal.ToUpper()}    {dataHora}    PÁGINA {paginaInicial} / {paginaFinal}");

            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);

            var style = new Style();
            style.SetFont(font);
            style.SetFontSize(8);
            style.SetFontColor(new DeviceRgb(0, 124, 191));

            style.SetPaddingBottom(8);
            text.AddStyle(style);

            var paragraph = new Paragraph(text);
            return paragraph;
        }

        private Paragraph CopiaProcessoParagrafo(string protocolo, string geradoPor, string dataHora, int paginaInicial, int paginaFinal)
        {
            var text = new Text($"E-DOCS - CÓPIA DO PROCESSO {protocolo.ToUpper()} GERADO POR {geradoPor.ToUpper()} EM {dataHora} PÁGINA {paginaInicial} / {paginaFinal}");

            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);

            var style = new Style();
            style.SetFont(font);
            style.SetFontSize(8);
            style.SetFontColor(ColorConstants.RED);

            style.SetPaddingTop(8);
            text.AddStyle(style);

            var paragraph = new Paragraph(text);
            return paragraph;
        }

        #endregion

    }

}
