using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CarInsuranceBot.Services
{
    /// <summary>
    /// Service responsible for interacting with the Groq Cloud API 
    /// to generate natural language responses using Large Language Models.
    /// </summary>
    public class GroqService : IGroqService
    {
        private readonly string _apiKey;
        private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";
        private const string ModelName = "llama-3.3-70b-versatile";

        public GroqService(IConfiguration config)
        {
            _apiKey = config["BotConfiguration:GroqApiKey"] 
                      ?? throw new Exception("Groq API Key is missing in configuration.");
        }

        /// <summary>
        /// Generates a personalized final congratulatory message for the user 
        /// after successful document processing.
        /// </summary>
        /// <returns>A string containing the AI-generated message or a fallback default.</returns>
        public async Task<string> GenerateFinalMessageAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                // System prompt to guide the AI personality and output constraints
                string prompt = "You are a polite and cheerful insurance agent. The client has successfully provided all documents " +
                                "(passport and vehicle registration), and their car insurance is approved. " +
                                "Write a short message (2-3 sentences) in Russian. Congratulate them on the successful issuance, " +
                                "mention that the electronic PDF document will be uploaded to this chat shortly, " +
                                "and wish them luck on the roads. Do not use Markdown formatting.";

                var requestBody = new
                {
                    model = ModelName,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(ApiUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Log API errors for internal debugging
                    Console.WriteLine($"Groq API Error: {responseString}");
                    
                    // Logical fallback if the AI service is unavailable
                    return "🎉 Поздравляем! Ваш страховой полис успешно оформлен. Электронный документ скоро появится в этом чате. Удачи на дорогах!";
                }

                using var document = JsonDocument.Parse(responseString);
                var reply = document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return reply ?? "Ваш полис готов!";
            }
            catch (Exception ex)
            {
                // Catch network or parsing exceptions
                Console.WriteLine($"Groq Service Exception: {ex.Message}");
                return "🎉 Ваш страховой полис успешно оформлен! Счастливого пути!";
            }
        }
    }
}