﻿using System;
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

        public async Task<byte[]> GetAndDownloadAsync(string url)
        {
            HttpClient _httpClient = HttpClientFactory.CreateClient("default");
            var result = await _httpClient.GetAsync(url);
            if (!result.IsSuccessStatusCode)
                throw new Exception(await result.Content.ReadAsStringAsync());

            byte[] bytes = await result.Content.ReadAsByteArrayAsync();

            return bytes;
        }
    }
}