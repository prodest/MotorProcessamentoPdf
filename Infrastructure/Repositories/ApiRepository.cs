using Business.Models;
using Infrastructure.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ApiRepository : IApiRepository
    {
        private readonly JsonData JsonData;
        private static string PDF_MOTOR_PROCESSAMENTO_PDF;

        public ApiRepository(JsonData jsonData, IConfiguration config)
        {
            JsonData = jsonData;
            PDF_MOTOR_PROCESSAMENTO_PDF = config.GetSection("APIItextSharp").Value;
        }

        public async Task<ApiResponse<IEnumerable<CertificadoDigitalDto>>> ValidarAssinaturaDigitalAsync(byte[] arquivo)
        {
            using MemoryStream memoryStream = new MemoryStream(arquivo);
            
            using StreamContent streamContent = new StreamContent(memoryStream);
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "arquivo",
                FileName = "arquivo.pdf"
            };

            using MultipartFormDataContent multipartContent = new MultipartFormDataContent();
            multipartContent.Add(streamContent, "arquivo");

            var result = await JsonData.PostAndReadObjectAsync<IEnumerable<CertificadoDigitalDto>>(
                PDF_MOTOR_PROCESSAMENTO_PDF + "/api/TransformaPdf/ValidarAssinaturaDigital",
                multipartContent
            );

            return result;
        }

        public async Task<ApiResponse<IEnumerable<CertificadoDigitalDto>>> ValidarAssinaturaDigitalAsync(string url)
        {
            using MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
            multipartFormDataContent.Add(new StringContent(url), "url");
                
            var result = await JsonData.PostAndReadObjectAsync<IEnumerable<CertificadoDigitalDto>>(
                PDF_MOTOR_PROCESSAMENTO_PDF + "/api/TransformaPdf/ValidarAssinaturaDigitalByUrl",
                multipartFormDataContent
            );
                
            return result;
        }
    }
}
