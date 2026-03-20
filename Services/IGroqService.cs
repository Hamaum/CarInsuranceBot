using System.Threading.Tasks;

namespace CarInsuranceBot.Services
{
    public interface IGroqService
    {
        Task<string> GenerateFinalMessageAsync();
        // НОВЫЙ МЕТОД: для генерации обычных ответов бота
        Task<string> GenerateBotResponseAsync(string prompt);
    }
}