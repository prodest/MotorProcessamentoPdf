using Business.Core.ICore;
using Business.Helpers;
using Business.Shared.Models;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Extgstate;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Business.Core
{
    public class CarimboCore : ICarimboCore
    {
        private readonly string dateFormat = "dd/MM/yyyy hh:mm";
        private readonly string identificadorEdocs = "<edocs>registro</edocs>";

        #region Adição de Carimbos

        public byte[] Documento(byte[] arquivo, string registro, int natureza, int valorLegal, DateTime dataHora)
        {
            // validações
            Validations.ArquivoValido(arquivo);
            Validations.RegistroValido(registro);
            Validations.dataHoraValida(dataHora);

            using (MemoryStream readingStream = new MemoryStream(arquivo))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (MemoryStream writingStream = new MemoryStream())
            using (PdfWriter pdfWriter = new PdfWriter(writingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader, pdfWriter))
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

                return writingStream.ToArray();
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

        public byte[] AdicionarTokenEdocs(byte[] arquivo, string registro)
        {
            // validações
            Validations.ArquivoValido(arquivo);

            using (MemoryStream readingStream = new MemoryStream(arquivo))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (MemoryStream writingStream = new MemoryStream())
            using (PdfWriter pdfWriter = new PdfWriter(writingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader, pdfWriter))
            using (Document document = new Document(pdfDocument))
            {
                Rectangle pageSize;
                PdfCanvas canvas;
                int n = pdfDocument.GetNumberOfPages();
                for (int i = 1; i <= n; i++)
                {
                    PdfPage page = pdfDocument.GetPage(i);
                    pageSize = page.GetPageSize();
                    canvas = new PdfCanvas(page);
                    // Desenhar Marca D'dágua
                    var text = identificadorEdocs.Replace("registro", registro);
                    Paragraph p = new Paragraph(text).SetFontSize(5);
                    canvas.SaveState();
                    PdfExtGState gs1 = new PdfExtGState().SetFillOpacity(0);
                    canvas.SetExtGState(gs1);
                    document.ShowTextAligned(
                        p,
                        0, 0,
                        pdfDocument.GetPageNumber(page),
                        TextAlignment.LEFT, VerticalAlignment.TOP,
                        0
                    );
                    canvas.RestoreState();
                }
                pdfDocument.Close();

                return writingStream.ToArray();
            }
        }

        #endregion

        #region Validações

        public ApiResponse<string> ValidarDocumentoDuplicado(byte[] arquivo)
        {
            var result = ValidarMetadadosEdocs(arquivo);
            if (result != null)
                return new ApiResponse<string>(200, "success", result);

            var token = ValidarTokenEdocs(arquivo);
            if(token != null)
                return new ApiResponse<string>(200, "success", result);

            return new ApiResponse<string>(200, "success", null);
        }

        private string ValidarMetadadosEdocs(byte[] arquivo)
        {
            // validações
            Validations.ArquivoValido(arquivo);

            using (MemoryStream readingStream = new MemoryStream(arquivo))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                var metadados = pdfDocument.GetXmpMetadata();
                if (metadados == null)
                    return null;

                string metadadosString = Encoding.UTF8.GetString(metadados);
                if (metadadosString.Contains("<xmp:CreatorTool>E-DOCS</xmp:CreatorTool>"))
                {
                    string texto = "E-DOCS, Documento capturado pelo E-DOCS, ";
                    string registroDocumento = metadadosString.Substring(metadadosString.IndexOf(texto) + texto.Length, 11);
                    return $"Este documento já foi capturado e está disponível no E-Docs sob registro: {registroDocumento}";
                }
                else
                    return null;
            }
        }

        private string ValidarTokenEdocs(byte[] arquivo)
        {
            // validações
            Validations.ArquivoValido(arquivo);

            int paginaValidada = 1;

            using (MemoryStream readingStream = new MemoryStream(arquivo))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                string text = PdfTextExtractor.GetTextFromPage(
                    pdfDocument.GetPage(paginaValidada),
                    new SimpleTextExtractionStrategy()
                );
                return BuscarIdentificadorEdocs(text);
            }
        }

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

        #region Auxiliares

        private string BuscarIdentificadorEdocs(string text)
        {
            var tokenIdentificadorEdocs = Regex.Match(text, "<edocs>.*</edocs>").Value;
            var registro = tokenIdentificadorEdocs
                .Replace("<edocs>", "")
                .Replace("</edocs>", "");
            var match = Regex.Match(registro.ToUpper(), "^20[0-9]{2}-([0-9B-DF-HJ-NP-TV-Z]){6}");
            if (match.Success)
                return match.Value;
            else
                return null;
        }

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
    }
}
