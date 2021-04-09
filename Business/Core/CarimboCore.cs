using Business.Core.ICore;
using Business.Helpers;
using Business.Shared.Models;
using Infrastructure;
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
        private readonly JsonData JsonData;

        public CarimboCore(JsonData jsonData)
        {
            JsonData = jsonData;
        }

        #region Adição de Carimbos

        #region Carimbo Lateral

        public async Task<byte[]> CarimboLateral(InputFile inputFile, string texto, float tamanhoFonte, 
            Margem margem, string cor, int? paginaInicial, int? totalPaginas
        ){
            inputFile.IsValid();

            byte[] response;
            if (!string.IsNullOrWhiteSpace(inputFile.FileUrl))
                response = await CarimboLateral(inputFile.FileUrl, texto, tamanhoFonte, margem, cor, paginaInicial, totalPaginas);
            else
                response = CarimboLateral(inputFile.FileBytes, texto, tamanhoFonte, margem, cor, paginaInicial, totalPaginas);

            return response;
        }

        private async Task<byte[]> CarimboLateral(string url, string texto, float tamanhoFonte, Margem margem, string cor, 
            int? paginaInicial, int? totalPaginas
        ){
            byte[] arquivo = await JsonData.GetAndReadByteArrayAsync(url);
            var resposta = CarimboLateral(arquivo, texto, tamanhoFonte, margem, cor, paginaInicial, totalPaginas);
            return resposta;
        }

        private byte[] CarimboLateral(byte[] arquivo, string texto, float tamanhoFonte, Margem margem, string cor, 
            int? paginaInicial, int? totalPaginas
        ){
            using MemoryStream readingStream = new MemoryStream(arquivo);
            using PdfReader pdfReader = new PdfReader(readingStream);

            using MemoryStream writingStream = new MemoryStream();
            using PdfWriter pdfWriter = new PdfWriter(writingStream);

            using PdfDocument pdfDocument = new PdfDocument(pdfReader, pdfWriter);

            if (paginaInicial == null)
                paginaInicial = 1;

            int numberOfPages = pdfDocument.GetNumberOfPages();
            if (totalPaginas == null)
                totalPaginas = numberOfPages;

            paginaInicial--;
            for (int i = 1; i <= numberOfPages; i++)
            {
                PdfPage page = pdfDocument.GetPage(i);
                page.SetIgnorePageRotationForContent(true);
                Rectangle pageSize = pdfDocument.GetPage(i).GetPageSizeWithRotation();
                Rectangle rectangle = new Rectangle(0, 0, 10, pageSize.GetHeight());

                using (Canvas canvas = new Canvas(page, rectangle))
                {
                    Paragraph paragraph = CriarParagrafo(texto, tamanhoFonte, margem, cor, pageSize.GetHeight(), paginaInicial + i, totalPaginas);
                    ConfigurarCanvas(canvas, pageSize, margem, paragraph, i);
                    canvas.Close();
                }
            }

            pdfDocument.Close();

            return writingStream.ToArray();
        }

        #region CarimboLateral: Auxiliares

        private void ConfigurarCanvas(Canvas canvas, Rectangle pageSize, Margem margem, Paragraph paragraph, int i)
        {
            if (margem == Margem.Esquerda)
            {
                canvas.ShowTextAligned(
                    paragraph,
                    0, pageSize.GetHeight() / 2,
                    i,
                    TextAlignment.CENTER, VerticalAlignment.TOP,
                    0.5f * (float)Math.PI
                );
            }
            else if (margem == Margem.Direita)
            {
                canvas.ShowTextAligned(
                    paragraph,
                    pageSize.GetWidth(),
                    pageSize.GetHeight() / 2,
                    i,
                    TextAlignment.CENTER, VerticalAlignment.BOTTOM,
                    0.5f * (float)Math.PI
                );
            }
            else
                throw new Exception("Valor de margem desconhecido.");
        }

        private Paragraph CriarParagrafo(string texto, float tamanhoFonte, Margem margem, string cor, float alturaPagina, int? paginaInicial, int? totalPaginas)
        {
            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            float fontSize;
            float padding;
            DefinirParametrosParagrafo(alturaPagina, tamanhoFonte, out fontSize, out padding);

            DeviceRgb color = Hexa2Rgb(cor);

            var style = new Style();
            style.SetFont(font);
            style.SetFontSize(fontSize);
            style.SetFontColor(color);
            
            if (margem == Margem.Esquerda)
                style.SetPaddingTop(padding);
            else if (margem == Margem.Direita)
                style.SetPaddingBottom(padding);
            else
                throw new Exception("Valor de margem desconhecido.");

            if (texto.Contains("{PaginaInicial}"))
                texto = texto.Replace("{PaginaInicial}", paginaInicial.ToString());
            if (texto.Contains("{TotalPaginas}"))
                texto = texto.Replace("{TotalPaginas}", totalPaginas.ToString());

            var text = new Text(texto);
            text.AddStyle(style);

            Paragraph paragraph = new Paragraph(text);
            paragraph.SetWidth(alturaPagina)
                .SetFixedLeading(fontSize + 1);

            return paragraph;
        }

        private void DefinirParametrosParagrafo(float alturaPagina, float tamanhoFonte, out float fontSize, out float padding)
        {
            fontSize = alturaPagina * tamanhoFonte;
            padding = alturaPagina * tamanhoFonte;

            if (fontSize > 8)
                fontSize = 8;

            if (padding > 8)
                padding = 8;
            if (padding < 1)
                padding = 1;
        }

        #endregion

        #endregion

        public byte[] AdicionarMarcaDagua(
            byte[] arquivo, string[] texto, int tamanhoFonte = 40, string corHexa = "ff0000",
            int anguloTextoGraus = 30, float opacidade = 0.1f, int repeticoes = 3
        )
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
                                .SetFontColor(Hexa2Rgb(corHexa));
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

        #endregion

        #region Validações

        public async Task<string> BuscarExpressoesRegulares(string url, IEnumerable<string> expressoesRegulares, IEnumerable<int> paginas)
        {
            var arquivo = await JsonData.GetAndReadByteArrayAsync(url);
            string response = BuscarExpressoesRegulares(arquivo, expressoesRegulares, paginas);
            return response;
        }

        public string BuscarExpressoesRegulares(byte[] arquivo, IEnumerable<string> expressoesRegulares, IEnumerable<int> paginas)
        {
            using (MemoryStream memoryStream = new MemoryStream(arquivo))
            {
                var result = BuscarExpressoesRegulares(memoryStream, expressoesRegulares, paginas);
                memoryStream.Close();
                return result;
            }
        }

        public string BuscarExpressoesRegulares(MemoryStream memoryStream, IEnumerable<string> expressoesRegulares, IEnumerable<int> paginas)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);

            using (PdfReader pdfReader = new PdfReader(memoryStream))
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
                    catch (Exception)
                    {
                        // Foi decidido que mesmo que o pdf apresente erros, o processo de leitura deve seguir adiante.
                        // Portanto, assumiu-se o risco de que pode haver algum texto que atenda a expressão regular, mas que este pode ser ignorado.
                    }
                }

                pdfDocument.Close();
                pdfReader.Close();

                return null;
            }
        }

        #endregion

        #region Auxiliares

        private DeviceRgb Hexa2Rgb(string corHexa)
        {
            if (corHexa.Length == 6)
            {
                int red = Convert.ToInt32(corHexa.Substring(0, 2), 16);
                int green = Convert.ToInt32(corHexa.Substring(2, 2), 16);
                int blue = Convert.ToInt32(corHexa.Substring(4, 2), 16);
                DeviceRgb deviceRgb = new DeviceRgb(red, green, blue);
                return deviceRgb;
            }
            else
                throw new Exception("Informe todos os 6 caracteres do código hexadecimal");
        }

        #endregion

    }
}