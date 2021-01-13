using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class JsonData
    {
        private readonly IHttpClientFactory HttpClientFactory;

        public JsonData(IHttpClientFactory httpClientFactory)
        {
            HttpClientFactory = httpClientFactory;
        }

        public async Task<byte[]> GetAndReadByteArrayAsync(string url)
        {
            HttpClient httpClient = HttpClientFactory.CreateClient("default");
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                throw new Exception(await response.Content.ReadAsStringAsync());

            var result = await response.Content.ReadAsByteArrayAsync();

            return result;
        }

        public async Task<T> GetAndReadObjectAsync<T>(string url)
        {
            HttpClient httpClient = HttpClientFactory.CreateClient("default");
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
                throw new Exception(await response.Content.ReadAsStringAsync());

            var result = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());

            return result;
        }

        public async Task<T> PostAndReadObjectAsync<T>(string url, HttpContent content)
        {
            HttpClient httpClient = HttpClientFactory.CreateClient("default");
            var response = await httpClient.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
                throw new Exception(await response.Content.ReadAsStringAsync());

            var result = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());

            return result;
        }
    }
}
