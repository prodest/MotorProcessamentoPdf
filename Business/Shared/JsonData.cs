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
                throw new Exception($"Não foi possível obter o documento.");
            }

            byte[] resultBytes;
            if (result.IsSuccessStatusCode)
                resultBytes = await result.Content.ReadAsByteArrayAsync();
            else
                throw new Exception($"Não foi possível obter o documento.");

            return resultBytes;
        }
    }
}
