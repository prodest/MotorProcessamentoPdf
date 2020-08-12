using Business.Core.ICore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
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
        public async Task<IActionResult> Copia([FromForm]PostObject postObject)
        {
            if (postObject.arquivo.Length > 0)
            {
                var arquivo = await ObterArquivo(postObject.arquivo);

                var arquivoCarimbado = CarimboCore.Copia(
                    arquivo,
                    postObject.codigo,
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
            public string codigo { get; set; }
            public string geradoPor { get; set; }
            public string dataHora { get; set; }
            public int paginaInicial { get; set; }
        }

        private async Task<byte[]> ObterArquivo(IFormFile ArquivoAnexo)
        {
            byte[] arquivoDados = null;

            //copia o IFormFile para byte array (referencia: https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-2.0 )
            using (var memoryStream = new MemoryStream())
            {
                await ArquivoAnexo.CopyToAsync(memoryStream);
                arquivoDados = memoryStream.ToArray();
            }

            return arquivoDados;
        }

        #endregion

    }
}
