using API.Tools;
using Business.Core.ICore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        [Consumes("multipart/form-data", "application/x-www-form-urlencoded")]
        public async Task<IActionResult> ValorLegal([FromForm] IFormFile arquivo, [FromForm]string registro, [FromForm] string valorLegal, [FromForm] string dataHora)
        {
            if (arquivo.Length > 0)
            {
                var arquivoByteArray = await PdfTools.ObterArquivo(arquivo);

                var arquivoCarimbado = CarimboCore.ValorLegal(
                    arquivoByteArray,
                    registro,
                    valorLegal,
                    dataHora
                );

                return File(arquivoCarimbado, "application/octet-stream");
            }

            return BadRequest();
        }

        [HttpPost]
        [Consumes("multipart/form-data", "application/x-www-form-urlencoded")]
        public async Task<IActionResult> CopiaProcesso([FromForm]PostObject postObject)
        {
            if (postObject.arquivo.Length > 0)
            {
                var arquivo = await PdfTools.ObterArquivo(postObject.arquivo);

                var arquivoCarimbado = CarimboCore.CopiaProcesso(
                    arquivo,
                    postObject.protocolo,
                    postObject.geradoPor,
                    postObject.dataHora,
                    postObject.paginaInicial
                );

                return File(arquivoCarimbado, "application/octet-stream");
            }

            return BadRequest();
        }

        #region Auxiliares

        public class PostObject 
        {
            public IFormFile arquivo { get; set; }
            public string protocolo { get; set; }
            public string geradoPor { get; set; }
            public string dataHora { get; set; }
            public int paginaInicial { get; set; }
        }

        #endregion

    }
}
