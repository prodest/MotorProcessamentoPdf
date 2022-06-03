using Business.Core.ICore;
using Business.Helpers;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Core
{
    public class ExtracaoCore : IExtracaoCore
    {
        private readonly IJsonData JsonData;

        public ExtracaoCore(IJsonData jsonData)
        {
            JsonData = jsonData;
        }

        public string ExtrairTextoConcatenado(byte[] arquivoBytes, IEnumerable<int> paginas)
        {
            // validações
            Validations.ArquivoValido(arquivoBytes);

            using (MemoryStream readingStream = new MemoryStream(arquivoBytes))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                if (paginas == null || paginas.Count() == 0)
                    paginas = new List<int>(Enumerable.Range(1, pdfDocument.GetNumberOfPages()));

                string texto = "";

                foreach (var pagina in paginas)
                {
                    texto = texto + PdfTextExtractor.GetTextFromPage(
                        pdfDocument.GetPage(pagina),
                        new SimpleTextExtractionStrategy()
                    );
                }

                return texto;
            }
        }

        public async Task<string> ExtrairTextoConcatenadoLink(string url, IEnumerable<int> paginas)
        {
            var arquivoBytes = await JsonData.GetAndReadByteArrayAsync(url);

            return ExtrairTextoConcatenado(arquivoBytes, paginas);
        }

        public List<KeyValuePair<int, string>> ExtrairTextoPorPaginas(byte[] arquivoBytes, IEnumerable<int> paginas)
        {
            // validações
            Validations.ArquivoValido(arquivoBytes);

            using (MemoryStream readingStream = new MemoryStream(arquivoBytes))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                if (paginas == null || paginas.Count() == 0)
                    paginas = new List<int>(Enumerable.Range(1, pdfDocument.GetNumberOfPages()));

                var result = new List<KeyValuePair<int, string>>();

                foreach (var pagina in paginas)
                {
                    string pageText = PdfTextExtractor.GetTextFromPage(
                        pdfDocument.GetPage(pagina),
                        new SimpleTextExtractionStrategy()
                    );

                    result.Add(new KeyValuePair<int, string>(pagina, pageText));
                }

                return result;
            }
        }

        public async Task<List<KeyValuePair<int, string>>> ExtrairTextoPorPaginasLink(string url, IEnumerable<int> paginas)
        {
            var arquivoBytes = await JsonData.GetAndReadByteArrayAsync(url);

            return ExtrairTextoPorPaginas(arquivoBytes, paginas);
        }
    }
}
