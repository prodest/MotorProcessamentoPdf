using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ApiRepository : IApiRepository
    {
        private readonly JsonData JsonData;
        private readonly IConfiguration Configuration;

        public ApiRepository(JsonData jsonData, IConfiguration configuration)
        {
            JsonData = jsonData;
            Configuration = configuration;
        }

        public async Task<string> OnlineChainValidationAsync(byte[] certificateBytes, bool ignoreExpired)
        {
            string urlValidarCertificado;
            if (ignoreExpired)
                urlValidarCertificado = Configuration["OutboundValidacaoCertificado"] + "/api/validar-certificado-ignorar-expirados";
            else
                urlValidarCertificado = Configuration["OutboundValidacaoCertificado"] + "/api/validar-certificado";

            using MemoryStream memoryStream = new MemoryStream(certificateBytes);

            using var MultipartFormDataContent = new MultipartFormDataContent();
            MultipartFormDataContent.Add(new StreamContent(memoryStream), "certificateFile", "certificateFile");

            string response = await JsonData.PostAndReadAsStringAsync(urlValidarCertificado, MultipartFormDataContent);

            return response;
        }

        public async Task<byte[]> GetAndReadAsByteArrayAsync(string url)
        {
            byte[] response = await JsonData.GetAndReadAsByteArrayAsync(url);
            return response;
        }
    }
}
