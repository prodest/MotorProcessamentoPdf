using Infrastructure.Models;
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

        public async Task<byte[]> GetAndReadAsByteArrayAsync(string url)
        {
            HttpClient httpClient = HttpClientFactory.CreateClient("multipart/form-data");
            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url);

            if (!httpResponseMessage.IsSuccessStatusCode)
                throw new Exception($"{httpResponseMessage.StatusCode}-{httpResponseMessage.Content.ReadAsStringAsync()}");

            byte[] response = await httpResponseMessage.Content.ReadAsByteArrayAsync();
            return response;
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

        public async Task<string> PostAndReadAsStringAsync(string url, HttpContent content)
        {
            HttpClient httpClient = HttpClientFactory.CreateClient("multipart/form-data");
            HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(url, content);

            string response = await httpResponseMessage.Content.ReadAsStringAsync();

            if (!httpResponseMessage.IsSuccessStatusCode)
                throw new Exception($"{httpResponseMessage.StatusCode}-{response}");
                
            return response;
        }

        public async Task<byte[]> PostAndReadAsByteArrayAsync(string url, HttpContent content)
        {
            HttpClient httpClient = HttpClientFactory.CreateClient("multipart/form-data");
            HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(url, content);

            if (!httpResponseMessage.IsSuccessStatusCode)
                throw new Exception($"{httpResponseMessage.StatusCode}-{httpResponseMessage.Content.ReadAsStringAsync()}");
                
            byte[] response = await httpResponseMessage.Content.ReadAsByteArrayAsync();
            return response;
        }
    }
}
