using Business.Core.ICore;
using Business.Handlers;
using Business.Helpers;
using Business.Shared.Models;
using Infrastructure;
using Infrastructure.Models;
using iText.Html2pdf;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Events;
using iText.Kernel.Exceptions;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Utils;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Pdfa;
using Newtonsoft.Json;
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
        private readonly JsonData JsonData;
        private readonly IAssinaturaDigitalCore AssinaturaDigitalCore;
        private readonly ICarimboCore CarimboCore;

        public TransformaPdfCore(JsonData jsonData, IAssinaturaDigitalCore assinaturaDigitalCore, ICarimboCore carimboCore)
        {
            JsonData = jsonData;
            AssinaturaDigitalCore = assinaturaDigitalCore;
            CarimboCore = carimboCore;
        }

        #region Validações

        public bool IsPdf(byte[] arquivo)
        {
            var result = Validations.IsPdf(arquivo);
            return result;
        }

        public bool IsPdfa1b(byte[] file)
        {
            Validations.IsPdfa1b(file);
            return true;
        }

        public async Task<bool> PossuiRestricoes(string url)
        {
            byte[] file;
            try
            {
                if (!string.IsNullOrWhiteSpace(url))
                    file = await JsonData.GetAndReadByteArrayAsync(url);
                else
                    throw new Exception("Não é possível ler este documento pois ele não é um arquivo PDF válido.");
            }
            catch (Exception)
            {
                throw;
            }

            using (MemoryStream readingStream = new MemoryStream(file))
            {
                try
                {
                    using (PdfReader pdfReader = new PdfReader(readingStream))
                    using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
                    {
                        return true;
                    }
                }
                catch (iText.IO.Exceptions.IOException e)
                {
                    throw new Exception("Não é possível ler este documento pois ele não é um arquivo PDF válido.");
                }
                catch (BadPasswordException e)
                {
                    throw new Exception("Não é possível ler este documento pois ele está protegido por senha.");
                }
                catch (Exception e)
                {
                    throw new Exception("Não é possível ler este documento pois ele possui restrições de acesso ao seu conteúdo.");
                }
            }
        }

        public bool PossuiRestricoes(byte[] file)
        {
            try
            {
                using MemoryStream memoryStream = new MemoryStream(file);
                using PdfReader pdfReader = new PdfReader(memoryStream);
                try
                {
                    using PdfDocument pdfDocument = new PdfDocument(pdfReader);
                    if (pdfReader.IsEncrypted())
                        throw new Exception("Não é possível ler este documento pois ele possui restrições de acesso ao seu conteúdo.");

                    pdfDocument.Close();
                    pdfReader.Close();
                    memoryStream.Close();

                    return true;
                }
                catch (BadPasswordException)
                {
                    throw new Exception("Não é possível ler este documento pois ele está protegido por senha.");
                }
            }
            catch (iText.IO.Exceptions.IOException)
            {
                throw new Exception("Não é possível ler este documento pois ele não é um arquivo PDF válido.");
            }
        }

        #endregion

        #region Outros

        #region PdfInfo

        public async Task<PdfInfo> PdfInfo(InputFile inputFile)
        {
            inputFile.IsValid();

            PdfInfo result = null;
            if (!string.IsNullOrWhiteSpace(inputFile.FileUrl))
                result = await PdfInfo(inputFile.FileUrl);
            else
                result = PdfInfo(inputFile.FileBytes);

            return result;
        }

        public async Task<PdfInfo> PdfInfo(string url)
        {
            byte[] arquivo = await JsonData.GetAndReadByteArrayAsync(url);
            var resposta = PdfInfo(arquivo);
            return resposta;
        }

        public PdfInfo PdfInfo(byte[] file)
        {
            Validations.ArquivoValido(file);

            using (MemoryStream memoryStream = new MemoryStream(file))
            {
                var pdfInfo = PdfInfo(memoryStream);
                memoryStream.Close();
                return pdfInfo;
            }
        }

        public PdfInfo PdfInfo(MemoryStream memoryStream)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);

            using (PdfReader pdfReader = new PdfReader(memoryStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                var fileInfo = new PdfInfo()
                {
                    NumberOfPages = pdfDocument.GetNumberOfPages(),
                    FileLength = pdfReader.GetFileLength()
                };

                pdfDocument.Close();
                pdfReader.Close();

                return fileInfo;
            }
        }

        #endregion

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

        public byte[] HtmlPdf(byte[] file)
        {
            var html = new MemoryStream(file);
            var output = new MemoryStream();
            HtmlConverter.ConvertToPdf(html, output);
            return output.ToArray();
        }

        public byte[] HtmlPdf(string html)
        {
            var output = new MemoryStream();
            HtmlConverter.ConvertToPdf(html, output);
            return output.ToArray();
        }

        public byte[] HtmlPdfCompleto(PdfRequest request)
        {
            using var outputStream = new MemoryStream();

            var writer = new PdfWriter(outputStream);
            var pdfDocument = new PdfDocument(writer);

            request.Page ??= PdfPageDefinition.Default();

            var pageSize = Helpers.Utils.GetPageSize(request.Page);
            pdfDocument.SetDefaultPageSize(pageSize);

            var totalPages = new PdfFormXObject(new Rectangle(0, 0, 20, 12));

            // HEADER
            if (!string.IsNullOrWhiteSpace(request.HtmlHeader))
            {
                pdfDocument.AddEventHandler(
                    PdfDocumentEvent.END_PAGE,
                    new PdfHeaderHandler(request.HtmlHeader, request.Page, totalPages)
                );
            }

            // FOOTER
            if (!string.IsNullOrWhiteSpace(request.HtmlFooter))
            {
                pdfDocument.AddEventHandler(
                    PdfDocumentEvent.END_PAGE,
                    new PdfFooterHandler(request.HtmlFooter, request.Page)
                );
            }

            // WATERMARK
            if (!string.IsNullOrWhiteSpace(request.HtmlWatermark))
            {
                pdfDocument.AddEventHandler(
                    PdfDocumentEvent.END_PAGE,
                    new PdfWatermarkHandler(request.HtmlWatermark)
                );
            }

            var converterProperties = new ConverterProperties();
            converterProperties.SetBaseUri("");

            var document = new Document(pdfDocument, pageSize);

            document.SetMargins(
                request.Page.MarginTop,
                request.Page.MarginRight,
                request.Page.MarginBottom,
                request.Page.MarginLeft
            );

            var elements = HtmlConverter.ConvertToElements(request.HtmlBody, converterProperties);

            foreach (var element in elements)
            {
                switch (element)
                {
                    case IBlockElement blockElement:
                        document.Add(blockElement);
                        break;

                    case Image image:
                        document.Add(image);
                        break;

                    case AreaBreak areaBreak:
                        document.Add(areaBreak);
                        break;
                }
            }

            int totalPageCount = pdfDocument.GetNumberOfPages();

            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var canvas = new Canvas(totalPages, pdfDocument);

            canvas.SetFont(font);
            canvas.SetFontSize(7);
            canvas.SetFontColor(new DeviceRgb(107, 114, 128)); // equivalente o #6b7280 - cinza
            canvas.ShowTextAligned(
                totalPageCount.ToString(),
                0,
                0,
                TextAlignment.LEFT
            );
            canvas.Close();

            document.Close();

            return outputStream.ToArray();
        }

        public byte[] PdfPagination(byte[] file, int itemsByPage, int page)
        {
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(new MemoryStream(file)));
            ICollection<byte[]> output = new CustomPdfSplitter(pdfDocument).SplitByPageCount(itemsByPage);
            pdfDocument.Close();
            return output.ElementAt(page);
        }

        #region PdfConcatenation

        public byte[] PdfConcatenation(IEnumerable<byte[]> files)
        {
            var outputMemoryStream = new MemoryStream();
            PdfDocument outputPdfDocument = null;

            try
            {
                // ConcatenacaoPdfWriter em vez de PdfWriter: limpa arrays com item nulo
                // na hora de gravar cada objeto, evitando o NullReferenceException que
                // o iText 7.2.0 lança ao percorrer o /Filter de streams copiados.
                outputPdfDocument = new PdfDocument(new ConcatenacaoPdfWriter(outputMemoryStream));

                foreach (var file in files)
                {
                    using (var fileMemoryStream = new MemoryStream(file))
                    using (var filePdfReader = new PdfReader(fileMemoryStream))
                    {
                        // ignorando as restrições de segurança do documento
                        // https://kb.itextpdf.com/home/it7kb/faq/how-to-read-pdfs-created-with-an-unknown-random-owner-password
                        filePdfReader.SetUnethicalReading(true);

                        // Cada origem é fechada aqui, ainda dentro do laço, de propósito:
                        // é o fechamento da origem que faz o iText gravar os objetos já
                        // copiados. Segurar todas as origens abertas até o fim acumula
                        // tudo para o Close() final e degrada muito a concatenação.
                        using (var filePdfDocument = new PdfDocument(filePdfReader))
                        {
                            filePdfDocument.CopyPagesTo(1, filePdfDocument.GetNumberOfPages(), outputPdfDocument);
                        }
                    }
                }

                // Close() do destino é chamado UMA única vez. Se este Close() falhar, a
                // exceção real precisa subir; um segundo Close() (por Dispose/using) veria
                // o catálogo já gravado e lançaria "Cannot close document with already
                // flushed PDF Catalog", mascarando o erro verdadeiro.
                outputPdfDocument.Close();
                outputPdfDocument = null;

                return outputMemoryStream.ToArray();
            }
            finally
            {
                // Só entra aqui se houve falha antes do Close() acima. Os catch vazios
                // impedem que uma falha de limpeza substitua a exceção original.
                if (outputPdfDocument != null)
                {
                    try { outputPdfDocument.Close(); } catch { /* documento incompleto */ }
                }

                outputMemoryStream.Dispose();
            }
        }

        public async Task<byte[]> PdfConcatenation(IEnumerable<string> urls)
        {
            List<byte[]> arquivos = new List<byte[]>();
            try
            {
                foreach (var url in urls)
                    arquivos.Add(await JsonData.GetAndReadByteArrayAsync(url));
            }
            catch (Exception)
            {
                throw new Exception($"Não foi possível obter o documento.");
            }

            var arquivoFinal = PdfConcatenation(arquivos);

            return arquivoFinal;
        }

        public async Task<byte[]> ConcatenarUrlEArquivo(string url, byte[] documentoMetadados)
        {
            byte[] documentoFromUrl;
            try
            {
                documentoFromUrl = await JsonData.GetAndReadByteArrayAsync(url);
            }
            catch (Exception)
            {
                throw new Exception($"Não foi possível obter o documento.");
            }

            var arquivoFinal = PdfConcatenation(new List<byte[]>() { documentoFromUrl, documentoMetadados });

            return arquivoFinal;
        }

        public async Task<ValidationsResult> Validacoes(string url, string validations)
        {
            byte[] documentoFromUrl = await JsonData.GetAndReadByteArrayAsync(url);

            ValidationsSelector validationsSelector =
                JsonConvert.DeserializeObject<ValidationsSelector>(validations);

            ValidationsResult result = new ValidationsResult();

            using (var memoryStream = new MemoryStream(documentoFromUrl))
            {
                result.IsPdf = true;
                result.PossuiRestricoesLeituraOuAlteracao = false;

                // Possui assinatura digital
                result.PossuiAssinaturaDigital = AssinaturaDigitalCore.HasDigitalSignature(memoryStream);

                // Possui carimbo edocs
                result.RegexResult = CarimboCore.BuscarExpressoesRegulares(
                    memoryStream,
                    validationsSelector.RegularExpressionsParameters.ExpressoesRegulares,
                    validationsSelector.RegularExpressionsParameters.Paginas
                );

                // Count regular expression matches
                if (validationsSelector.RegularExpressionsCounter)
                {
                    result.RegexMatchesCounter = await CarimboCore.RegularExpressionMatchCounter(
                        new InputFile() { FileUrl = url },
                        validationsSelector.RegularExpressionsParameters.ExpressoesRegulares.First()
                    );
                }

                // Obter informações sobre o pdf
                result.PdfInfo = PdfInfo(memoryStream);

                memoryStream.Close();
            }

            return result;
        }

        #endregion

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
