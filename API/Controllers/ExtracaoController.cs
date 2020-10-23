using API.Tools;
using Business.Core.ICore;
using Business.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ExtracaoController : ControllerBase
    {
        private readonly IExtracaoCore ExtracaoCore;

        public ExtracaoController(IExtracaoCore extracaoCore)
        {
            ExtracaoCore = extracaoCore;
        }

        [HttpPost]
        public async Task<IActionResult> ExtrairTextoPorPaginas(IFormFile arquivo, [FromForm] IEnumerable<int> paginas)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await PdfTools.ObterArquivo(arquivo);
                var response = ExtracaoCore.ExtrairTextoPorPaginas(arquivoBytes, paginas);
                return Ok(new ApiResponse<List<KeyValuePair<int, string>>>(200, "success", response));
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> ExtrairTextoConcatenado(IFormFile arquivo, [FromForm] IEnumerable<int> paginas)
        {
            if (arquivo.Length > 0)
            {
                var arquivoBytes = await PdfTools.ObterArquivo(arquivo);
                var response = ExtracaoCore.ExtrairTextoConcatenado(arquivoBytes, paginas);
                return Ok(new ApiResponse<string>(200, "success", response));
            }

            return BadRequest();
        }
    }
}
