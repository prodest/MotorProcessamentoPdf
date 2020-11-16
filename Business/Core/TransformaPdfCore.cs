﻿using Business.Core.ICore;
using Business.Helpers;
using Business.Shared;
using Business.Shared.Models;
using iText.Html2pdf;
using iText.Kernel.Crypto;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Pdfa;
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

        public TransformaPdfCore(JsonData jsonData)
        {
            JsonData = jsonData;
        }

        #region Validações

        public bool IsPdf(byte[] file)
        {
            var isPdf = Validations.IsPdf(file);
            return isPdf;
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

        public async Task<byte[]> PdfConcatenation(IEnumerable<string> urls)
        {
            List<byte[]> arquivos = new List<byte[]>();
            try
            {
                foreach (var url in urls)
                    arquivos.Add(await JsonData.GetAndDownloadAsync(url));
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao obter os arquivos através das urls.\n{ex.Message}");
            }

            var arquivoFinal = PdfConcatenation(arquivos);

            return arquivoFinal;
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

        //public async Task<byte[]> Merge(IEnumerable<MergeItem> items)
        //{
        //    byte[] arquivos = null;
        //    if (items?.Count() > 0)
        //    {
        //        foreach (var item in items)
        //            if(!string.IsNullOrWhiteSpace(item.Url) && item.Arquivo?.Length == 0)
        //                item.Arquivo = await JsonData.GetAndDownloadAsync(item.Url);

        //        arquivos = PdfConcatenation(items.OrderBy(x => x.Ordem).Select(x => x.Arquivo));
        //    }

        //    return arquivos;
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
