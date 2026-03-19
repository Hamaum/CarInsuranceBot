namespace CarInsuranceBot.Models
{
    /// <summary>
    /// Represents the various states of the user conversation flow within the bot.
    /// </summary>
    public enum BotState
    {
        /// <summary> Initial state before any interaction. </summary>
        Start,

        /// <summary> User is expected to upload a passport image. </summary>
        WaitingForPassport,

        /// <summary> User is expected to upload a vehicle registration document. </summary>
        WaitingForVehicleDocument,

        /// <summary> User needs to confirm the accuracy of the extracted passport data. </summary>
        WaitingForDataConfirmation,

        /// <summary> Final step: User confirms vehicle data before policy generation. </summary>
        WaitingForPaymentConfirmation
    }
}