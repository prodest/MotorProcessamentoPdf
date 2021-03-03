using API.Models;
using API.Tools;
using AutoMapper;
using Business.Core.ICore;
using Business.Shared.Models;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    public class TransformaPdfController : BaseApiController
    {
        private readonly ITransformaPdfCore TransformaPdfCore;
        private readonly IAssinaturaDigitalCore AssinaturaDigitalCore;
        private readonly IMapper Mapper;

        public TransformaPdfController(ITransformaPdfCore transformaPdfCore, IAssinaturaDigitalCore assinaturaDigitalCore, IMapper mapper)
        {
            TransformaPdfCore = transformaPdfCore;
            AssinaturaDigitalCore = assinaturaDigitalCore;
            Mapper = mapper;
        }

        #region Validações

        [HttpPost]
        public async Task<IActionResult> Validacoes([FromForm] string url, [FromForm] string validacoes)
        {
            var result = await TransformaPdfCore.Validacoes(url, validacoes);
            return Ok(new ApiResponse<ValidationsResult>(200, "success", result));
        }

        [HttpPost]
        public async Task<IActionResult> IsPdf(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await PdfTools.ObterArquivo(arquivo);
                var result = TransformaPdfCore.IsPdf(arquivoBytes);
                return Ok(new ApiResponse<bool>(200, "success", result));
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> IsPdfa(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await PdfTools.ObterArquivo(arquivo);
                TransformaPdfCore.IsPdf(arquivoBytes);
                TransformaPdfCore.IsPdfa1b(arquivoBytes);

                return Ok();
            }

            return BadRequest();
        }

        #region Validar Restricoes Leitura Ou Alteracao

        [HttpPost]
        public async Task<IActionResult> ValidarRestricoesLeituraOuAltaretacao(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoByteArray = await PdfTools.ObterArquivo(arquivo);
                var response = TransformaPdfCore.PossuiRestricoes(arquivoByteArray);
                return Ok(new ApiResponse<bool>(200, "success", response));
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> ValidarRestricoesLeituraOuAlteracaoByUrl([FromForm] string url)
        {
            var response = await TransformaPdfCore.PossuiRestricoes(url);
            return Ok(new ApiResponse<bool>(200, "success", response));
        }

        #endregion

        #region Possui Restricoes

        [HttpPost]
        public async Task<IActionResult> PossuiRestricoes(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoByteArray = await PdfTools.ObterArquivo(arquivo);
                var response = TransformaPdfCore.PossuiRestricoes(arquivoByteArray);
                return Ok(new ApiResponse<bool>(200, "success", response));
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> PossuiRestricoesByUrl([FromForm] string url)
        {
            var response = await TransformaPdfCore.PossuiRestricoes(url);
            return Ok(new ApiResponse<bool>(200, "success", response));
        }

        #endregion

        #region Assinatura Digital

        #region Possui Assinatura Digital

        [HttpPost]
        public IActionResult HasDigitalSignature(IFormFile arquivo)
        {
            var file = Mapper.Map<byte[]>(arquivo);
            var response = AssinaturaDigitalCore.HasDigitalSignature(file);
            return Ok(new ApiResponse<object>(200, "success", response));
        }

        [HttpPost]
        public async Task<IActionResult> HasDigitalSignatureByUrl([FromForm] string url)
        {
            var response = await AssinaturaDigitalCore.HasDigitalSignature(url);
            return Ok(new ApiResponse<object>(200, "success", response));
        }

        #endregion

        #region Validar Assinatura Digital

        [HttpPost]
        public async Task<IActionResult> ValidarAssinaturaDigital(IFormFile arquivo)
        {
            var arquivoBytes = Mapper.Map<byte[]>(arquivo);
            var result = await AssinaturaDigitalCore.SignatureValidation(arquivoBytes);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> ValidarAssinaturaDigitalByUrl([FromForm] string url)
        {
            var result = await AssinaturaDigitalCore.SignatureValidation(url);
            return Ok(result);
        }

        #endregion

        #region Adicionar Assinatura Digital

        [HttpPost]
        public async Task<IActionResult> AdicionarAssinaturaDigital([FromForm]InputFileDto inputFileDto, [FromForm]string signatureFieldName)
        {
            var inputFile = Mapper.Map<InputFile>(inputFileDto);
            var documentoAssinado = await AssinaturaDigitalCore.AdicionarAssinaturaDigital(inputFile, signatureFieldName);
            return File(documentoAssinado, "application/octet-stream"); ;
        }

        #endregion

        #endregion

        #endregion

        #region Outros

        #region PDF Info

        [HttpPost]
        public async Task<IActionResult> InputFileByInputFile([FromForm]InputFileDto inputFileDto)
        {
            InputFile inputFile = Mapper.Map<InputFile>(inputFileDto);
            var response = await TransformaPdfCore.PdfInfo(inputFile);
            return Ok(new ApiResponse<PdfInfo>(200, "success", response));
        }

        [HttpPost]
        public async Task<IActionResult> PdfInfoUrl([FromForm] string url)
        {
            var response = await TransformaPdfCore.PdfInfo(url);
            return Ok(new ApiResponse<PdfInfo>(200, "success", response));
        }

        [HttpPost]
        public async Task<IActionResult> PdfInfo(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoByteArray = await PdfTools.ObterArquivo(arquivo);
                var response = TransformaPdfCore.PdfInfo(arquivoByteArray);
                return Ok(new ApiResponse<PdfInfo>(200, "success", response));
            }

            return BadRequest();
        }

        #endregion

        #region Concatenar Pdfs

        [HttpPost]
        public async Task<IActionResult> ConcatenarPdfs(IFormFileCollection arquivos)
        {
            if (arquivos.Count() > 1)
            {
                var arquivosBytes = await PdfTools.ObterArquivos(arquivos);
                var output = TransformaPdfCore.PdfConcatenation(arquivosBytes);

                return File(output, "application/octet-stream");
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> ConcatenarPdfsByUrl([FromForm] IEnumerable<string> urls)
        {
            var output = await TransformaPdfCore.PdfConcatenation(urls);
            return File(output, "application/octet-stream");
        }

        [HttpPost]
        public async Task<IActionResult> ConcatenarUrlEArquivo([FromForm] string url, IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                byte[] documentoBytes = await PdfTools.ObterArquivo(arquivo);
                var output =  await TransformaPdfCore.ConcatenarUrlEArquivo(url, documentoBytes);
                return File(output, "application/octet-stream");
            }

            return BadRequest();
        }

        #endregion

        [HttpPost]
        public IActionResult HtmlPdf([FromForm]string html)
        {
            var output = TransformaPdfCore.HtmlPdf(html);
            return Ok(new ApiResponse<byte[]>(200, "success", output));
        }

        [HttpPost]
        public async Task<IActionResult> HtmlPdfByFile(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await PdfTools.ObterArquivo(arquivo);
                var output = TransformaPdfCore.HtmlPdf(arquivoBytes);
                return File(output, "application/octet-stream");
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> RemoverAnotacoes(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await PdfTools.ObterArquivo(arquivo);
                var arquivoLimpo = TransformaPdfCore.RemoveAnnotations(arquivoBytes);

                return File(arquivoLimpo, "application/octet-stream");
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> PaginacaoDePDF(IFormFile arquivo, [FromForm] int itensPorPagina, [FromForm] int pagina)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await PdfTools.ObterArquivo(arquivo);
                var output = TransformaPdfCore.PdfPagination(arquivoBytes, itensPorPagina, pagina);

                return File(output, "application/octet-stream");
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> MetaPDFA(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await PdfTools.ObterArquivo(arquivo);
                var output = TransformaPdfCore.MetaPDFA(arquivoBytes);

                return File(output, "application/octet-stream");
            }

            return BadRequest();
        }

        #endregion
    }
}
