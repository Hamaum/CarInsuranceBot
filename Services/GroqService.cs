using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CarInsuranceBot.Services
{
    public class GroqService : IGroqService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GroqService> _logger;

        public GroqService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["BotConfiguration:GroqApiKey"] ?? throw new ArgumentNullException("GroqApiKey is missing");
            _logger = logger;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            // Groq API совместим с OpenAI форматом
            _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        }

        public async Task<string> GenerateBotResponseAsync(string prompt)
        {
            return await SendGroqRequestAsync(prompt, "You are a helpful, polite, and professional AI Car Insurance Assistant.");
        }

        public async Task<string> GenerateFinalMessageAsync()
        {
            string prompt = "Generate a formal and creative dummy car insurance policy for the user. Include a policy number, coverage details (standard 100 USD fixed price), and a polite thank you message for choosing our service.";
            return await SendGroqRequestAsync(prompt, "You are a professional insurance agent issuing a policy.");
        }

        private async Task<string> SendGroqRequestAsync(string userPrompt, string systemPrompt)
        {
            try
            {
                var requestBody = new
                {
                    model = "llama-3.1-8b-instant", // Заменили на актуальную и самую быструю модель Groq
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.7
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("chat/completions", content);

                // Если сервер вернул ошибку (например, 400 Bad Request)
                if (!response.IsSuccessStatusCode)
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Groq API Error Details (Status {response.StatusCode}): {errorDetails}");
                    return "I apologize, my AI core rejected the request. Please try again.";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                var reply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                return reply?.Trim() ?? "I'm sorry, I couldn't generate a response.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Groq Internal Error: {ex.Message}");
                return "I apologize, but my AI core is currently unavailable. Please try again in a moment.";
            }
        }
    }
}