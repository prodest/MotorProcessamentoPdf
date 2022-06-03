using APIItextSharp.Models;
using AutoMapper;
using Business.Models;
using BusinessItextSharp.Core;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIItextSharp.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AssinaturaDigitalController : ControllerBase
    {
        private readonly IAssinaturaDigitalCore AssinaturaDigitalCore;
        private readonly IMapper Mapper;

        public AssinaturaDigitalController(IAssinaturaDigitalCore assinaturaDigitalCore, IMapper mapper)
        {
            AssinaturaDigitalCore = assinaturaDigitalCore;
            Mapper = mapper;
        }

        #region Possui Assinatura Digital

        [HttpPost]
        public async Task<IActionResult> HasDigitalSignature(IFormFile arquivo)
        {
            var arquivoByteArray = await Mapper.Map<Task<byte[]>>(arquivo);
            var response = AssinaturaDigitalCore.HasDigitalSignature(arquivoByteArray);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> HasDigitalSignatureByUrl([FromForm] string url)
        {
            var response = await AssinaturaDigitalCore.HasDigitalSignature(url);
            return Ok(response);
        }

        #endregion

        [HttpPost]
        public async Task<IActionResult> ValidarAssinaturaDigital([FromForm]InputFileDto inputFileDto, [FromForm] bool ignorarExpiradas)
        {
            var inputFile = await Mapper.Map<Task<InputFile>>(inputFileDto);
            var result = await AssinaturaDigitalCore.SignatureValidation(inputFile, ignorarExpiradas);
            return Ok(new ApiResponse<IEnumerable<CertificadoDigitalDto>>(200, "success", result));
        }

        [HttpGet]
        [Route("/api/ObterInformacoesCertificadoDigital")]
        public IActionResult ObterInformacoesCertificadoDigital()
        {
            var certificado = AssinaturaDigitalCore.ObterInformacoesCertificadoDigital();
            return Ok(certificado);
        }  
    }
}
