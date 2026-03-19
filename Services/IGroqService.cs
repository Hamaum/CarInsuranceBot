namespace CarInsuranceBot.Services
{
    /// <summary>
    /// Defines the contract for a service that interacts with the Groq AI platform.
    /// Used for generating natural language content within the insurance workflow.
    /// </summary>
    public interface IGroqService
    {
        /// <summary>
        /// Asynchronously generates a personalized congratulatory message for the user.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, containing the generated text.</returns>
        Task<string> GenerateFinalMessageAsync();
    }
}