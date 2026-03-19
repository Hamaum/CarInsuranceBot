using CarInsuranceBot.Models;

namespace CarInsuranceBot.Services
{
    /// <summary>
    /// Provides an abstraction for managing user sessions and conversation states.
    /// This allows for easy swapping between in-memory storage and persistent databases.
    /// </summary>
    public interface ISessionService
    {
        /// <summary>
        /// Retrieves the session associated with the specific chat ID.
        /// If no session exists, a new one should be initialized.
        /// </summary>
        /// <param name="chatId">The unique Telegram identifier for the user chat.</param>
        /// <returns>The <see cref="UserSession"/> object containing the user's progress.</returns>
        UserSession GetSession(long chatId);

        /// <summary>
        /// Persists or updates the current state of a user session in the storage provider.
        /// </summary>
        /// <param name="session">The session object to be synchronized.</param>
        void UpdateSession(UserSession session);
    }
}