﻿using API.Tools;
using AutoMapper;
using Business.Core.ICore;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
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

        #region Adição de Carimbos

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
        public async Task<IActionResult> Documento(IFormFile arquivo, [FromForm] string registro, [FromForm] int natureza, [FromForm] int valorLegal, [FromForm] DateTime dataHora)
        {
            var arquivoBytes = await Mapper.Map<Task<byte[]>>(arquivo);
            var arquivoCarimbado = CarimboCore.Documento(
                arquivoBytes,
                registro,
                natureza,
                valorLegal,
                dataHora);
            return Ok(new ApiResponse<byte[]>(200, "success", arquivoCarimbado));
        }

        [HttpPost]
        public async Task<IActionResult> DocumentoByFile(IFormFile arquivo, [FromForm] string registro, [FromForm] int natureza, [FromForm] int valorLegal, [FromForm] DateTime dataHora)
        {
            var arquivoBytes = await Mapper.Map<Task<byte[]>>(arquivo);
            var arquivoCarimbado = CarimboCore.Documento(
                arquivoBytes,
                registro,
                natureza,
                valorLegal,
                dataHora);
            return File(arquivoCarimbado, "application/pdf");
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
        public async Task<IActionResult> CopiaProcessoByFile(IFormFile arquivo, [FromForm] string protocolo, [FromForm] string geradoPor, [FromForm] DateTime dataHora, [FromForm] int totalPaginas, [FromForm] int paginaInicial)
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

                return File(arquivoCarimbado, "application/pdf");
            }
            else
                return BadRequest();
        }

        #endregion

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
