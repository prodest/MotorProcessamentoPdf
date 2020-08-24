using Business.Core.ICore;
using Business.Helpers;
using iText.IO.Font.Constants;
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
        private readonly string dateFormat = "dd/MM/yyyy hh:mm";

        public byte[] Documento(byte[] arquivo, string registro, int natureza, int valorLegal, DateTime dataHora)
        {
            // validações
            Validations.ArquivoValido(arquivo);
            Validations.RegistroValido(registro);
            Validations.dataHoraValida(dataHora);

            using (MemoryStream inputStream = new MemoryStream(arquivo))
            using (PdfReader reader = new PdfReader(inputStream))
            using (MemoryStream outputStream = new MemoryStream())
            using (PdfWriter writer = new PdfWriter(outputStream))
            using (PdfDocument pdfDocument = new PdfDocument(reader, writer))
            {
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    PdfPage page = pdfDocument.GetPage(i);
                    Rectangle rectangle = new Rectangle(0, 0, 10, page.GetPageSize().GetHeight());
                    using (Canvas canvas = new Canvas(page, rectangle))
                    {
                        var paragraph = ValorLegalParagrafo(
                            registro,
                            CarimboDocumentoHelper.CarimboDocumento(natureza, valorLegal),
                            dataHora.ToString(dateFormat),
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
                }

                pdfDocument.Close();

                return outputStream.ToArray();
            }
        }

        public byte[] CopiaProcesso(byte[] arquivo, string protocolo, string geradoPor, DateTime dataHora, int totalPaginas, int paginaInicial = 1)
        {
            // validações
            Validations.ArquivoValido(arquivo);
            Validations.ProtocoloValido(protocolo);
            GeradoPorValido(geradoPor);
            Validations.dataHoraValida(dataHora);
            TotalPaginasValido(totalPaginas);
            PaginaInicialValida(paginaInicial, totalPaginas);

            using (MemoryStream inputStream = new MemoryStream(arquivo))
            using (PdfReader reader = new PdfReader(inputStream))
            using (MemoryStream outputStream = new MemoryStream())
            using (PdfWriter writer = new PdfWriter(outputStream))
            using (PdfDocument pdfDocument = new PdfDocument(reader, writer))
            {
                paginaInicial--;
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    PdfPage page = pdfDocument.GetPage(i);
                    Rectangle rectangle = new Rectangle(0, 0, 10, page.GetPageSize().GetHeight());
                    using (Canvas canvas = new Canvas(page, rectangle))
                    {
                        var paragraph = CopiaProcessoParagrafo(
                            protocolo,
                            geradoPor,
                            dataHora.ToString(dateFormat),
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
                }

                pdfDocument.Close();

                return outputStream.ToArray();
            }
        }

        #region Auxiliares

        private Paragraph ValorLegalParagrafo(string protocolo, string valorLegal, string dataHora, int paginaInicial, int paginaFinal)
        {
            var text = new Text($"{protocolo.ToUpper()} - E-DOCS - {valorLegal.ToUpper()}    {dataHora}    PÁGINA {paginaInicial} / {paginaFinal}");

            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

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

            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

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

        #region Validações

        private void PaginaInicialValida(int paginaInicial, int totalPaginas)
        {
            if (paginaInicial <= 0)
                throw new Exception("O número da página inicial da cópia de processo precisa ser maior que zero.");
            if (paginaInicial > totalPaginas)
                throw new Exception("O número da página inicial da cópia de processo não pode ser superior ao número total de páginas.");
        }

        private void TotalPaginasValido(int totalPaginas)
        {
            if (totalPaginas <= 0)
                throw new Exception("O número total de páginas da cópia de processo precisa ser maior que zero.");
        }

        private void GeradoPorValido(string geradoPor)
        {
            if (string.IsNullOrWhiteSpace(geradoPor))
                throw new Exception("O nome do solicitante da cópia de processo está vazio.");
        }

        #endregion
    }

}
