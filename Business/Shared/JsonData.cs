using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Business.Shared
{
    public class JsonData
    {
        private readonly IHttpClientFactory HttpClientFactory;

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
                resultString =  await result.Content.ReadAsStringAsync();
            else
                throw new Exception(await result.Content.ReadAsStringAsync());

            return resultString;
        }
    }
}
