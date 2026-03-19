using System.Collections.Concurrent;
using CarInsuranceBot.Models;

namespace CarInsuranceBot.Services
{
    /// <summary>
    /// In-memory implementation of the session management service.
    /// Uses a thread-safe dictionary to track user progress and extracted data.
    /// </summary>
    public class InMemorySessionService : ISessionService
    {
        /// <summary>
        /// A thread-safe collection mapping Telegram Chat IDs to their respective user sessions.
        /// </summary>
        private readonly ConcurrentDictionary<long, UserSession> _sessions = new();

        /// <summary>
        /// Retrieves an existing session for the given chat ID or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="chatId">The unique identifier for the Telegram chat.</param>
        /// <returns>A <see cref="UserSession"/> object representing the user's current state.</returns>
        public UserSession GetSession(long chatId)
        {
            // Thread-safe: Get existing session or initialize a new one with BotState.Start
            return _sessions.GetOrAdd(chatId, id => new UserSession { ChatId = id });
        }

        /// <summary>
        /// Synchronizes and updates the provided session data in the global session store.
        /// </summary>
        /// <param name="session">The user session object to be updated.</param>
        public void UpdateSession(UserSession session)
        {
            // Updates the session entry; if the key doesn't exist, it adds it.
            _sessions.AddOrUpdate(session.ChatId, session, (key, oldValue) => session);
        }
    }
}