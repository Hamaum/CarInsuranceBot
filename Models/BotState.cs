namespace CarInsuranceBot.Models
{
    public enum BotState
    {
        Start,
        WaitingForPassport,
        WaitingForDataConfirmation,    // Добавили, так как это есть в коде (строка 104)
        WaitingForVehicleDocument,
        WaitingForPaymentConfirmation, // Добавили, так как это есть в коде (строка 118)
        WaitingForPriceConfirmation    // Наше новое состояние для цены
    }
}