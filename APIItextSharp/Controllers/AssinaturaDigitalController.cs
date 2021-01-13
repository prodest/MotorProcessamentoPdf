using AutoMapper;
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
        public IActionResult HasDigitalSignature(IFormFile arquivo)
        {
            var arquivoByteArray = Mapper.Map<byte[]>(arquivo);
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

        #region Validar Assinatura Digital

        [HttpPost]
        [Route("/api/TransformaPdf/ValidarAssinaturaDigital")]
        public async Task<IActionResult> ValidarAssinaturaDigital(IFormFile arquivo)
        {
            var arquivoByteArray = Mapper.Map<byte[]>(arquivo);
            var result = await AssinaturaDigitalCore.SignatureValidation(arquivoByteArray);
            var certificadoDigitalDto = Mapper.Map<IEnumerable<CertificadoDigitalDto>>(result);
            return Ok(new ApiResponse<IEnumerable<CertificadoDigitalDto>>(200, "success", certificadoDigitalDto));
        }

        [HttpPost]
        [Route("/api/TransformaPdf/ValidarAssinaturaDigitalByUrl")]
        public async Task<IActionResult> ValidarAssinaturaDigitalByUrl([FromForm] string url)
        {
            var result = await AssinaturaDigitalCore.SignatureValidation(url);
            var certificadoDigitalDto = Mapper.Map<IEnumerable<CertificadoDigitalDto>>(result);
            return Ok(new ApiResponse<IEnumerable<CertificadoDigitalDto>>(200, "success", certificadoDigitalDto));
        }

        #endregion

        [HttpGet]
        [Route("/api/ObterInformacoesCertificadoDigital")]
        public IActionResult ObterInformacoesCertificadoDigital()
        {
            var certificado = AssinaturaDigitalCore.ObterInformacoesCertificadoDigital();
            return Ok(certificado);
        }  
    }
}
