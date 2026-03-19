namespace CarInsuranceBot.Models
{
    /// <summary>
    /// Represents a persistent user session within the insurance bot workflow.
    /// Stores the conversation state and temporary extracted data from documents.
    /// </summary>
    public class UserSession
    {
        /// <summary> Unique Telegram Chat Identifier. </summary>
        public long ChatId { get; set; }

        /// <summary> Current step in the insurance processing state machine. </summary>
        public BotState CurrentState { get; set; } = BotState.Start;

        /// <summary> Extracted First Name from the identity document. </summary>
        public string? ExtractedFirstName { get; set; }

        /// <summary> Extracted Last Name from the identity document. </summary>
        public string? ExtractedLastName { get; set; }

        /// <summary> Extracted Passport Serial/ID number. </summary>
        public string? ExtractedPassportNumber { get; set; }

        /// <summary> Extracted Vehicle Registration/Plate number. </summary>
        public string? ExtractedVehiclePlate { get; set; }

        /// <summary>
        /// Resets all extracted metadata fields while keeping the session active.
        /// Useful for data correction flows or restart scenarios.
        /// </summary>
        public void ClearData()
        {
            ExtractedFirstName = null;
            ExtractedLastName = null;
            ExtractedPassportNumber = null;
            ExtractedVehiclePlate = null;
        }
    }
}