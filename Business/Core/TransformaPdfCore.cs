﻿using Business.Core.ICore;
using Business.Helpers;
using Business.Shared;
using Business.Shared.Models;
using iText.Html2pdf;
using iText.Kernel.Crypto;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Pdfa;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ITextIOException = iText.IO.IOException;

namespace Business.Core
{
    public class TransformaPdfCore : ITransformaPdfCore
    {
        private const string Intent = "./wwwroot/resources/color/sRGB_CS_profile.icm";
        private readonly JsonData JsonData;
        private readonly IAssinaturaDigitalCore AssinaturaDigitalCore;
        private readonly ICarimboCore CarimboCore;
        private readonly IExtracaoCore ExtracaoCore;

        public TransformaPdfCore(JsonData jsonData, IAssinaturaDigitalCore assinaturaDigitalCore, ICarimboCore carimboCore, IExtracaoCore extracaoCore)
        {
            JsonData = jsonData;
            AssinaturaDigitalCore = assinaturaDigitalCore;
            CarimboCore = carimboCore;
            ExtracaoCore = extracaoCore;
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
                    file = await JsonData.GetAndDownloadAsync(url);
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
                catch (iText.IO.IOException e)
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
                using (MemoryStream fileMemoryStream = new MemoryStream(file))
                using (PdfReader filePdfReader = new PdfReader(fileMemoryStream))
                using (PdfDocument filePdfDocument = new PdfDocument(filePdfReader))
                {
                    filePdfDocument.Close();
                    filePdfReader.Close();
                    fileMemoryStream.Close();

                    return true;
                }
            }
            catch (ITextIOException)
            {
                throw new Exception("Não é possível ler este documento pois ele não é um arquivo PDF válido.");
            }
            catch (BadPasswordException)
            {
                throw new Exception("Não é possível ler este documento pois ele está protegido por senha.");
            }
            catch (Exception)
            {
                throw new Exception("Não é possível ler este documento pois ele possui restrições de acesso ao seu conteúdo.");
            }
        }

        #endregion

        #region Outros

        public async Task<ApiResponse<PdfInfo>> PdfInfo(string url)
        {
            byte[] arquivo = await JsonData.GetAndDownloadAsync(url);
            var resposta = PdfInfo(arquivo);
            return resposta;
        }

        public ApiResponse<PdfInfo> PdfInfo(byte[] file)
        {
            Validations.ArquivoValido(file);

            using (MemoryStream readingStream = new MemoryStream(file))
            using (PdfReader pdfReader = new PdfReader(readingStream))
            using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
            {
                var fileInfo = new PdfInfo()
                {
                    NumberOfPages = pdfDocument.GetNumberOfPages(),
                    FileLength = pdfReader.GetFileLength()
                };

                return new ApiResponse<PdfInfo>(200, "success", fileInfo);
            }
        }

        private PdfInfo PdfInformation(byte[] file)
        {
            using (MemoryStream memoryStream = new MemoryStream(file))
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
                memoryStream.Close();

                return fileInfo;
            }
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
            using (var outputMemoryStream = new MemoryStream())
            using (var outputPdfWriter = new PdfWriter(outputMemoryStream))
            using (var outputPdfDocument = new PdfDocument(outputPdfWriter))
            {
                foreach (var file in files)
                {
                    using (var fileMemoryStream = new MemoryStream(file))
                    using (var filePdfReader = new PdfReader(fileMemoryStream))
                    {
                        // ignorando as restrições de segurança do documento
                        // https://kb.itextpdf.com/home/it7kb/faq/how-to-read-pdfs-created-with-an-unknown-random-owner-password
                        filePdfReader.SetUnethicalReading(true);
                        using (var filePdfDocument = new PdfDocument(filePdfReader))
                        {
                            filePdfDocument.CopyPagesTo(1, filePdfDocument.GetNumberOfPages(), outputPdfDocument);
                            filePdfDocument.Close();
                        }
                        filePdfReader.Close();
                        fileMemoryStream.Close();
                    }
                }

                outputPdfDocument.Close();
                outputPdfWriter.Close();
                outputMemoryStream.Close();

                return outputMemoryStream.ToArray();
            }
        }

        public async Task<byte[]> PdfConcatenation(IEnumerable<string> urls)
        {
            List<byte[]> arquivos = new List<byte[]>();
            try
            {
                foreach (var url in urls)
                    arquivos.Add(await JsonData.GetAndDownloadAsync(url));
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
                documentoFromUrl = await JsonData.GetAndDownloadAsync(url);
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
            var documentoFromUrl = await JsonData.GetAndDownloadAsync(url);

            var validationsSelector = JsonConvert.DeserializeObject<ValidationsSelector>(validations);

            var result = new ValidationsResult();

            if (validationsSelector.IsPdf)
                result.IsPdf = IsPdf(documentoFromUrl);

            if (validationsSelector.PossuiRestricoesLeituraOuAlteracao)
                result.PossuiRestricoesLeituraOuAlteracao = !PossuiRestricoes(documentoFromUrl);
         
            if (validationsSelector.PossuiAssinaturaDigital)
                result.PossuiAssinaturaDigital = AssinaturaDigitalCore.HasDigitalSignature(documentoFromUrl);
            
            if (validationsSelector.PossuiCarimboEdocs)
            {
                // Expressões regulares para identificação de carimbos
                string ExpressaoCarimboDocumentoCapturado = "20[0-9]{2}-[0-9B-DF-HJ-NP-TV-Z]{6} - E-DOCS .* [0-9]{2}/[0-9]{2}/20[0-9]{2} [0-9]{2}:[0-9]{2} .* PÁGINA";
                string ExpressaoCarimboCopiaProcesso = "E-DOCS - CÓPIA DO PROCESSO 20[0-9]{2}-[0-9B-DF-HJ-NP-TV-Z]{5} GERADO POR .* [0-9]{2}/[0-9]{2}/20[0-9]{2} [0-9]{2}:[0-9]{2} PÁGINA";

                var saida = CarimboCore.BuscarExpressoesRegulares(
                    documentoFromUrl,
                    new List<string>() { ExpressaoCarimboDocumentoCapturado, ExpressaoCarimboCopiaProcesso },
                    null);

                if (!string.IsNullOrWhiteSpace(saida))
                    result.PossuiCarimboEdocs = true;
                else
                    result.PossuiCarimboEdocs = false;
            }

            if (validationsSelector.PdfInfo)
                result.PdfInfo = PdfInformation(documentoFromUrl);

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
