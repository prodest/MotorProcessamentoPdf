using Business.Core.ICore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TransformaPdfController : ControllerBase
    {
        private readonly ITransformaPdfCore TransformaPdfCore;

        public TransformaPdfController(ITransformaPdfCore transformaPdfCore)
        {
            TransformaPdfCore = transformaPdfCore;
        }

        [HttpPost]
        [Consumes("multipart/form-data", "application/x-www-form-urlencoded")]
        public async Task<IActionResult> PaginacaoDePDF([FromForm]IFormFile arquivo, [FromForm] int itensPorPagina, [FromForm] int pagina)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await ObterArquivo(arquivo);
                
                var output = TransformaPdfCore.PdfPagination(arquivoBytes, itensPorPagina, pagina);

                return File(output, "application/octet-stream");
            }

            return BadRequest();
        }

        [HttpPost]
        [Consumes("multipart/form-data", "application/x-www-form-urlencoded")]
        public async Task<IActionResult> HtmlPdf([FromForm] IFormFile arquivo)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await ObterArquivo(arquivo);

                var output = TransformaPdfCore.HtmlPdf(arquivoBytes);

                return File(output, "application/octet-stream");
            }

            return BadRequest();
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
    }
}
