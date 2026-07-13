using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DeepSeekBatchTool.Utils;

namespace DeepSeekBatchTool.Core
{
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxConcurrent;

        public ApiClient(string apiKey = null, string model = null, int maxConcurrent = 3)
        {
            _apiKey = apiKey ?? ConfigHelper.ApiKey;
            _model = model ?? ConfigHelper.ModelName;
            _maxConcurrent = maxConcurrent > 0 ? maxConcurrent : ConfigHelper.MaxConcurrent;
            _semaphore = new SemaphoreSlim(_maxConcurrent);
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
        }

        public async Task<string> SendRequestAsync(string userInput, string systemPrompt)
        {
            await _semaphore.WaitAsync();
            try
            {
                var requestBody = new
                {
                    model = _model,
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userInput }
                    },
                    temperature = 0.7,
                    max_tokens = 4096
                };

                string json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await _httpClient.PostAsync("https://api.deepseek.com/v1/chat/completions", content);
                string responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ API报错：{response.StatusCode} - {responseJson}";
                }

                dynamic obj = JsonConvert.DeserializeObject(responseJson);
                string result = obj.choices[0].message.content;
                return result;
            }
            catch (Exception ex)
            {
                return $"❌ 异常：{ex.Message}";
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _semaphore?.Dispose();
        }
    }
}