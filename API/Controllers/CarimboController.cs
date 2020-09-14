using API.Tools;
using Business.Core.ICore;
using Business.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CarimboController : ControllerBase
    {
        private readonly ICarimboCore CarimboCore;

        public CarimboController(ICarimboCore carimboCore)
        {
            CarimboCore = carimboCore;
        }

        #region Adição de Carimbos

        [HttpPost]
        public async Task<IActionResult> Documento(IFormFile arquivo, [FromForm] string registro, [FromForm] int natureza, [FromForm] int valorLegal, [FromForm] DateTime dataHora)
        {
            if (arquivo.Length > 0)
            {
                var arquivoByteArray = await PdfTools.ObterArquivo(arquivo);

                var arquivoCarimbado = CarimboCore.Documento(
                    arquivoByteArray,
                    registro,
                    natureza,
                    valorLegal,
                    dataHora
                );

                return Ok(new ApiResponse<byte[]>(200, "success", arquivoCarimbado));
            }
            else
                return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> CopiaProcesso(IFormFile arquivo, [FromForm] string protocolo, [FromForm] string geradoPor, [FromForm] DateTime dataHora, [FromForm] int totalPaginas, [FromForm] int paginaInicial)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await PdfTools.ObterArquivo(arquivo);

                var arquivoCarimbado = CarimboCore.CopiaProcesso(
                    arquivoBytes,
                    protocolo,
                    geradoPor,
                    dataHora,
                    totalPaginas,
                    paginaInicial
                );

                return Ok(new ApiResponse<byte[]>(200, "success", arquivoCarimbado));
            }
            else
                return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarTokenEdocs(IFormFile arquivo, [FromForm] string registro)
        {
            if (arquivo.Length > 0)
            {
                var arquivoByteArray = await PdfTools.ObterArquivo(arquivo);
                var arquivoCarimbado = CarimboCore.AdicionarTokenEdocs(arquivoByteArray, registro);
                return Ok(new ApiResponse<byte[]>(200, "success", arquivoCarimbado));
            }
            else
                return BadRequest();
        }

        #endregion

        #region Validações

        [HttpPost]
        public async Task<IActionResult> ValidarDocumentoDuplicado(IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await PdfTools.ObterArquivo(arquivo);
                var result = CarimboCore.ValidarDocumentoDuplicado(arquivoBytes);
                return Ok(result);
            }

            return BadRequest();
        }

        #endregion
    }
}
