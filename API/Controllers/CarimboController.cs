using API.Tools;
using Business.Core.ICore;
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

        [HttpPost]
        public async Task<IActionResult> Documento(IFormFile arquivo, [FromForm]string registro, [FromForm]int natureza, [FromForm]int valorLegal, [FromForm]DateTime dataHora)
        {
            var arquivoByteArray = await PdfTools.ObterArquivo(arquivo);
            
            var arquivoCarimbado = CarimboCore.Documento(
                arquivoByteArray,
                registro,
                natureza,
                valorLegal,
                dataHora
            );

            return File(arquivoCarimbado, "application/octet-stream");
        }

        [HttpPost]
        public async Task<IActionResult> CopiaProcesso(IFormFile arquivo, [FromForm]string protocolo, [FromForm]string geradoPor, [FromForm]DateTime dataHora, [FromForm]int totalPaginas, [FromForm]int paginaInicial)
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

            return File(arquivoCarimbado, "application/octet-stream");
        }
    }
}
