using Business.Core.ICore;
using Business.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class JsonData : IJsonData
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

        public async Task<ApiResponse<T>> PostAndReadObjectAsync<T>(string url, HttpContent content)
        {
            HttpClient httpClient = HttpClientFactory.CreateClient("default");
            var result = await httpClient.PostAsync(url, content);

            string responseContent = await result.Content.ReadAsStringAsync();

            ApiResponse<T> apiResponse;
            if (result.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(responseContent))
                apiResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(responseContent);
            else
            {
                var apiResponseDeserialize = JsonConvert.DeserializeObject<ApiResponse<object>>(responseContent);
                if (apiResponseDeserialize != null)
                    apiResponse = new ApiResponse<T>(apiResponseDeserialize);
                else
                    throw new Exception($"{result.StatusCode}-{responseContent}");
            }

            return apiResponse;
        }
    }
}
