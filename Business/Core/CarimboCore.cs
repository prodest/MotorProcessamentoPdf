using Business.Core.ICore;
using Business.Helpers;
using Business.Shared;
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Business.Core
{
    public class CarimboCore : ICarimboCore
    {
        private readonly string dateFormat = "dd/MM/yyyy hh:mm";
        private readonly string identificadorEdocs = "<edocs>registro</edocs>";
        private readonly JsonData JsonData;

        public CarimboCore(JsonData jsonData)
        {
            JsonData = jsonData;
        }

        #region Adição de Carimbos

        public byte[] Documento(MemoryStream arquivo, string registro, int natureza, int valorLegal, DateTime dataHora)
        {
            // validações
            ValidationHelper.ArquivoValido(arquivo);
            ValidationHelper.RegistroValido(registro);
            ValidationHelper.DataHoraValida(dataHora);

            arquivo.Seek(0, SeekOrigin.Begin);

            using (PdfReader pdfReader = new PdfReader(arquivo))
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
            ValidationHelper.ArquivoValido(arquivo);
            ValidationHelper.ProtocoloValido(protocolo);
            GeradoPorValido(geradoPor);
            ValidationHelper.DataHoraValida(dataHora);
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

        public byte[] AdicionarMarcaDagua(
            byte[] arquivo, string[] texto, int tamanhoFonte = 40, string corHexa = "ff0000",
            int anguloTextoGraus = 30, float opacidade = 0.1f, int repeticoes = 3
        )
        {
            // validações
            ValidationHelper.ArquivoValido(arquivo);

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
                float angleRads = (anguloTextoGraus * (float)Math.PI) / 180;
                for (int pageCounter = 1; pageCounter <= n; pageCounter++)
                {
                    PdfPage page = pdfDocument.GetPage(pageCounter);
                    pageSize = page.GetPageSize();
                    canvas = new PdfCanvas(page);

                    int k = 1;
                    for (int repetitionCounter = 0; repetitionCounter < repeticoes; repetitionCounter++)
                    {
                        for (int textCounter = 0; textCounter < texto.Length; textCounter++)
                        {
                            // Desenhar Marca D'dágua
                            Paragraph p = new Paragraph(texto[textCounter])
                                .SetFontSize(tamanhoFonte)
                                .SetFontColor(FromHexa2Rgb(corHexa));
                            canvas.SaveState();
                            PdfExtGState gs1 = new PdfExtGState().SetFillOpacity(opacidade);
                            canvas.SetExtGState(gs1);
                            document.ShowTextAligned(
                                p,
                                pageSize.GetWidth() / 2,
                                (pageSize.GetHeight() / ((texto.Length * repeticoes) + 1)) * k++,
                                pdfDocument.GetPageNumber(page),
                                TextAlignment.CENTER, VerticalAlignment.MIDDLE,
                                angleRads
                            );
                        }
                    }
                    canvas.RestoreState();
                }
                pdfDocument.Close();

                return writingStream.ToArray();
            }
        }

        public byte[] AdicionarTokenEdocs(byte[] arquivo, string registro)
        {
            // validações
            ValidationHelper.ArquivoValido(arquivo);

            using (MemoryStream readingStream = new MemoryStream(arquivo))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (MemoryStream writingStream = new MemoryStream())
            using (PdfWriter pdfWriter = new PdfWriter(writingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader, pdfWriter))
            using (Document document = new Document(pdfDocument))
            {
                PdfCanvas canvas;
                int n = pdfDocument.GetNumberOfPages();

                for (int i = 1; i <= n; i++)
                {
                    PdfPage page = pdfDocument.GetPage(i);
                    var pageSize = page.GetPageSize();
                    canvas = new PdfCanvas(page);
                    // Desenhar Marca D'dágua
                    var text = identificadorEdocs.Replace("registro", registro);
                    Paragraph p = new Paragraph(text).SetFontColor(ColorConstants.RED).SetFontSize(0.1f);
                    canvas.SaveState();
                    PdfExtGState gs1 = new PdfExtGState().SetFillOpacity(0);
                    canvas.SetExtGState(gs1);
                    document.ShowTextAligned(
                        p,
                        pageSize.GetWidth()/2, pageSize.GetHeight()/2,
                        pdfDocument.GetPageNumber(page),
                        TextAlignment.LEFT, VerticalAlignment.BOTTOM,
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

        public async Task<string> BuscarExpressoesRegulares(string url, IEnumerable<string> expressoesRegulares, IEnumerable<int> paginas)
        {
            var arquivo = await JsonData.GetAndDownloadAsync(url);
            string response = BuscarExpressoesRegulares(arquivo, expressoesRegulares, paginas);
            return response;
        }

        public string BuscarExpressoesRegulares(byte[] arquivo, IEnumerable<string> expressoesRegulares, IEnumerable<int> paginas)
        {
            ValidationHelper.ArquivoValido(arquivo);

            using (MemoryStream readingStream = new MemoryStream(arquivo))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                if (paginas == null || paginas.Count() == 0)
                    paginas = new List<int>(Enumerable.Range(1, pdfDocument.GetNumberOfPages()));

                foreach (var pagina in paginas)
                {
                    try
                    {
                        string pageText = PdfTextExtractor.GetTextFromPage(
                            pdfDocument.GetPage(pagina),
                            new SimpleTextExtractionStrategy()
                        );

                        foreach (var regexItem in expressoesRegulares)
                        {
                            var registro = Regex.Match(pageText, regexItem);

                            if (registro.Success)
                                return registro.Value;
                        }
                    }
                    catch(ArgumentException ex)
                    {
                        if (!ex.Message.Contains("is not a supported encoding name"))
                            throw;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("Error at file pointer"))
                            throw new Exception("Documento corrompido: Erro ao validar a presença de carimbo no documento.");
                        else
                            throw;
                    }

                }

                return null;
            }
        }

        #endregion

        #region Auxiliares

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

        private DeviceRgb FromHexa2Rgb(string corHexa)
        {
            if (corHexa.Length == 6)
            {
                int red = Convert.ToInt32(corHexa.Substring(0, 2), 16);
                int green = Convert.ToInt32(corHexa.Substring(2, 2), 16);
                int blue = Convert.ToInt32(corHexa.Substring(4, 2), 16);
                return new DeviceRgb(red, green, blue);
            }
            else
                throw new Exception("Informe todos os 6 caracteres do código hexadecimal");
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

        #region Outros

        private string ValidarMetadadosEdocs(byte[] arquivo)
        {
            // validações
            ValidationHelper.ArquivoValido(arquivo);

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

        private string BuscarCarimboDocumento(byte[] arquivo)
        {
            // validações
            ValidationHelper.ArquivoValido(arquivo);

            int paginaValidada = 1;

            using (MemoryStream readingStream = new MemoryStream(arquivo))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                string text = PdfTextExtractor.GetTextFromPage(
                    pdfDocument.GetPage(paginaValidada),
                    new SimpleTextExtractionStrategy()
                );

                var carimboDocumento = Regex.Match(text, "20[0-9]{2}-[0-9B-DF-HJ-NP-TV-Z]{6} - E-DOCS");
                if (carimboDocumento.Success)
                {
                    var possivelCarimbo = text.Substring(carimboDocumento.Index, text.Length - carimboDocumento.Index);
                    var carimbo = Regex.Match(possivelCarimbo, "20[0-9]{2}-[0-9B-DF-HJ-NP-TV-Z]{6} - E-DOCS .* [0-9]{2}/[0-9]{2}/20[0-9]{2} [0-9]{2}:[0-9]{2} .* PÁGINA");
                    if (carimbo.Success)
                    {
                        return $"Este documento já foi capturado e está disponível no E-Docs sob registro: {possivelCarimbo.Substring(0, 11)}";
                    }
                }

                return null;
            }
        }

        private string BuscarCarimboCopiaProcesso(byte[] arquivo)
        {
            // validações
            ValidationHelper.ArquivoValido(arquivo);

            int paginaValidada = 1;

            using (MemoryStream readingStream = new MemoryStream(arquivo))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                string text = PdfTextExtractor.GetTextFromPage(
                    pdfDocument.GetPage(paginaValidada),
                    new SimpleTextExtractionStrategy()
                );

                var carimboCopiaProcesso = Regex.Match(text, "E-DOCS - CÓPIA DO PROCESSO 20[0-9]{2}-[0-9B-DF-HJ-NP-TV-Z]{5}");
                if (carimboCopiaProcesso.Success)
                {
                    var possivelCarimbo = text.Substring(carimboCopiaProcesso.Index, text.Length - carimboCopiaProcesso.Index);
                    var carimbo = Regex.Match(possivelCarimbo, "E-DOCS - CÓPIA DO PROCESSO 20[0-9]{2}-[0-9B-DF-HJ-NP-TV-Z]{5} GERADO POR .* [0-9]{2}/[0-9]{2}/20[0-9]{2} [0-9]{2}:[0-9]{2} PÁGINA");
                    if (carimbo.Success)
                    {
                        return $"Não é permitido capturar uma Cópia de Processo";
                    }
                }

                return null;
            }
        }

        #endregion
    }
}
