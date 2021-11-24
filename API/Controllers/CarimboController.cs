using API.Models;
using API.Tools;
using AutoMapper;
using Business.Core.ICore;
using Business.Shared.Models;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    public class CarimboController : BaseApiController
    {
        private readonly ICarimboCore CarimboCore;
        private readonly IMapper Mapper;

        public CarimboController(ICarimboCore carimboCore, IMapper mapper)
        {
            CarimboCore = carimboCore;
            Mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarMarcaDagua(
            IFormFile arquivo, [FromForm] string[] texto, [FromForm] int tamanhoFonte = 40, [FromForm] string corHexa = "ff0000",
            [FromForm] int anguloTextoGraus = 30, [FromForm] float opacidade = 0.1f, [FromForm] int repeticoes = 3
        )
        {
            if (arquivo.Length > 0)
            {
                var arquivoByteArray = await PdfTools.ObterArquivo(arquivo);
                var arquivoMarcado = CarimboCore.AdicionarMarcaDagua(
                    arquivoByteArray,
                    texto,
                    tamanhoFonte,
                    corHexa,
                    anguloTextoGraus,
                    opacidade,
                    repeticoes
                );

                return Ok(new ApiResponse<byte[]>(200, "success", arquivoMarcado));
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> AdicionarMarcaDaguaByFile(
            IFormFile arquivo, [FromForm] string[] texto, [FromForm] int tamanhoFonte = 40, [FromForm] string corHexa = "ff0000",
             [FromForm] int anguloTextoGraus = 30, [FromForm] float opacidade = 0.1f,  [FromForm] int repeticoes = 3
        )
        {
            if (arquivo.Length > 0)
            {
                var arquivoByteArray = await PdfTools.ObterArquivo(arquivo);
                var arquivoMarcado = CarimboCore.AdicionarMarcaDagua(
                    arquivoByteArray,
                    texto,
                    tamanhoFonte,
                    corHexa,
                    anguloTextoGraus,
                    opacidade,
                    repeticoes
                );

                return File(arquivoMarcado, "application/pdf");
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> CarimboLateral(
            [FromForm] InputFileDto inputFileDto, [FromForm] string texto, [FromForm] Margem margem,
            [FromForm] string cor, [FromForm] int? paginaInicial = null, [FromForm] int? totalPaginas = null
        )
        {
            var inputFile = await Mapper.Map<Task<InputFile>>(inputFileDto);
            var documentoAssinado = await CarimboCore.CarimboLateral(inputFile, texto, 0.01f, margem, cor, paginaInicial, totalPaginas);
            return File(documentoAssinado, "application/octet-stream"); ;
        }

        [HttpPost]
        public async Task<IActionResult> CarimboLateralNovo(
            [FromForm] InputFileDto inputFileDto, [FromForm] string texto, [FromForm] float tamanhoFonte, 
            [FromForm] Margem margem, [FromForm] string cor, 
            [FromForm] int? paginaInicial = null, [FromForm] int? totalPaginas = null
        )
        {
            var inputFile = await Mapper.Map<Task<InputFile>>(inputFileDto);
            var documentoAssinado = await CarimboCore.CarimboLateral(inputFile, texto, tamanhoFonte, margem, cor, paginaInicial, totalPaginas);
            return File(documentoAssinado, "application/octet-stream");
        }

        [HttpPost]
        public async Task<IActionResult> SubstituirExpressaoRegularTexto([FromForm] InputFileDto inputFileDto, [FromForm] IEnumerable<string> expressoesRegulares, [FromForm] string texto)
        {
            InputFile inputFile = await Mapper.Map<Task<InputFile>>(inputFileDto);

            byte[] documentoAssinado = await CarimboCore.SubstituirExpressaoRegularPorTexto(inputFile, expressoesRegulares, texto);

            return File(documentoAssinado, "application/octet-stream");
        }

        #region Validações

        [HttpPost]
        public async Task<IActionResult> BuscarExpressoesRegularesByUrl([FromForm] string url, [FromForm] IEnumerable<string> expressoesRegulares, [FromForm] IEnumerable<int> paginas)
        {
            var response = await CarimboCore.BuscarExpressoesRegulares(url, expressoesRegulares, paginas);
            return Ok(new ApiResponse<string>(200, "success", response));
        }

        [HttpPost]
        public async Task<IActionResult> BuscarExpressoesRegulares(IFormFile arquivo, [FromForm] IEnumerable<string> expressoesRegulares, [FromForm] IEnumerable<int> paginas)
        {
            if (arquivo.Length > 0) // analisar de tirar essa validacao aqui e jogar para dentro de obter arquivo
            {
                var arquivoBytes = await PdfTools.ObterArquivo(arquivo);
                var response = CarimboCore.BuscarExpressoesRegulares(arquivoBytes, expressoesRegulares, paginas);
                return Ok(new ApiResponse<string>(200, "success", response));
            }

            return BadRequest();
        }

        #endregion
    }
}
