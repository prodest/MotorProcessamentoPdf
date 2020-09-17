using API.Tools;
using Business.Core.ICore;
using Business.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TransformaPdfController : ControllerBase
    {
        private readonly ITransformaPdfCore TransformaPdfCore;
        private readonly IAssinaturaDigitalCore AssinaturaDigitalCore;

        public TransformaPdfController(ITransformaPdfCore transformaPdfCore, IAssinaturaDigitalCore assinaturaDigitalCore)
        {
            TransformaPdfCore = transformaPdfCore;
            AssinaturaDigitalCore = assinaturaDigitalCore;
        }

        // https://docs.microsoft.com/pt-br/aspnet/core/web-api/?view=aspnetcore-3.1#binding-source-parameter-inference

        #region Validações

        [HttpPost]
        public async Task<IActionResult> IsPdf(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await PdfTools.ObterArquivo(arquivo);
                TransformaPdfCore.IsPdf(arquivoBytes);

                return Ok(new ApiResponse<string>(200, "success", null));
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

        [HttpPost]
        public async Task<IActionResult> ValidarAssinaturaDigital(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoByteArray = await PdfTools.ObterArquivo(arquivo);
                await AssinaturaDigitalCore.SignatureValidation(arquivoByteArray);
                
                return Ok();
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> ValidarRestricoesLeituraOuAltaretacao(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoByteArray = await PdfTools.ObterArquivo(arquivo);
                var response = TransformaPdfCore.ValidarRestricoesLeituraOuAltaretacao(arquivoByteArray);
                return Ok(response);
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> PdfInfo(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoByteArray = await PdfTools.ObterArquivo(arquivo);
                var response = TransformaPdfCore.PdfInfo(arquivoByteArray);
                return Ok(response);
            }

            return BadRequest();
        }

        #endregion

        #region Outros

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
        public async Task<IActionResult> ConcatenarPdfsUsingMinio([FromForm] IEnumerable<string> arquivos)
        {
            if (arquivos.Count() > 1)
            {
                var output = await TransformaPdfCore.PdfConcatenationUsingMinio(arquivos);

                return File(output, "application/octet-stream");
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> HtmlPdf(IFormFile arquivo)
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
