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
            this.HttpClientFactory = httpClientFactory;
        }

        public async Task<byte[]> GetAndDownloadAsync(string url)
        {
            HttpClient _httpClient = HttpClientFactory.CreateClient();
            var result = await _httpClient.GetAsync(url);

            if (!result.IsSuccessStatusCode)
                throw new Exception(await result.Content.ReadAsStringAsync());

            byte[] bytes = await result.Content.ReadAsByteArrayAsync();

            return bytes;
        }
    }
}
