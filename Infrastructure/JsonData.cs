using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class JsonData
    {
        private readonly IHttpClientFactory HttpClientFactory;
        private string ApiValidarCertificado = @"https://api.es.gov.br/certificado/api/validar-certificado";

        public JsonData(IHttpClientFactory httpClientFactory)
        {
            HttpClientFactory = httpClientFactory;
        }

        public async Task<byte[]> GetAndDownloadAsync(string url)
        {
            HttpClient httpClient = HttpClientFactory.CreateClient();

            HttpResponseMessage result;
            try
            {
                result = await httpClient.GetAsync(url);
            }
            catch (Exception)
            {
                throw new Exception("Erro ao executar requisição HTTP.");
            }

            byte[] resultBytes;
            if (result.IsSuccessStatusCode)
                resultBytes = await result.Content.ReadAsByteArrayAsync();
            else
                throw new Exception($"Não foi possível obter o documento.");

            return resultBytes;
        }

        public async Task<string> PostAndReadStreamContentAsync(string url, HttpContent content)
        {
            var httpClient = HttpClientFactory.CreateClient("multipart/form-data");

            HttpResponseMessage result;
            try
            {
                result = await httpClient.PostAsync(url, content);
            }
            catch (Exception)
            {
                throw new Exception("Erro ao executar requisição HTTP.");
            }

            string resultString;
            if (result.IsSuccessStatusCode)
                resultString = await result.Content.ReadAsStringAsync();
            else
                throw new Exception(await result.Content.ReadAsStringAsync());

            return resultString;
        }

        public async Task<HttpResponseMessage> Upload(string url, byte[] pdf)
        {
            using (var client = new HttpClient())
            using (var stream = new MemoryStream(pdf))
            {
                var multipartContent = new MultipartFormDataContent()
                {
                    { new StreamContent(stream), "certificateFile", "sadfsdafsdafsda" }
                };
                return await client.PostAsync(url, multipartContent);
            }
        }

        public async Task<string> OnlineChainValidation(byte[] certificate)
        {
            using (MemoryStream memoryStream = new MemoryStream(certificate))
            using (StreamContent streamContent = new StreamContent(memoryStream))
            using (MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent())
            {
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "certificateFile",
                    FileName = "certificateFile"
                };

                multipartFormDataContent.Add(streamContent, "certificateFile", "certificateFile");

                var result = await PostAndReadStreamContentAsync(ApiValidarCertificado, multipartFormDataContent);
                return result;
            }
        }
    }
}
