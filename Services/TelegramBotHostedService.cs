using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using CarInsuranceBot.Models;

namespace CarInsuranceBot.Services
{
    /// <summary>
    /// Background service that manages the Telegram Bot lifecycle.
    /// Implements a state-machine logic to guide users through the insurance application process.
    /// </summary>
    public class TelegramBotHostedService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ISessionService _sessionService;
        private readonly IMindeeService _mindeeService;
        private readonly IGroqService _groqService;
        private readonly ILogger<TelegramBotHostedService> _logger;

        public TelegramBotHostedService(
            ITelegramBotClient botClient,
            ISessionService sessionService,
            IMindeeService mindeeService,
            IGroqService groqService,
            ILogger<TelegramBotHostedService> logger)
        {
            _botClient = botClient;
            _sessionService = sessionService;
            _mindeeService = mindeeService;
            _groqService = groqService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // Receive all update types
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandlePollingErrorAsync,
                receiverOptions,
                stoppingToken
            );

            var me = await _botClient.GetMe(stoppingToken);
            _logger.LogInformation("Bot @{Username} is running successfully.", me.Username);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message || update.Message == null)
                return;

            var message = update.Message;
            var chatId = message.Chat.Id;
            var session = _sessionService.GetSession(chatId);

            // --- 1. HANDLE TEXT UPDATES ---
            if (message.Type == MessageType.Text)
            {
                await HandleTextUpdateAsync(botClient, message, session, cancellationToken);
            }
            // --- 2. HANDLE PHOTO UPDATES ---
            else if (message.Type == MessageType.Photo)
            {
                await HandlePhotoUpdateAsync(botClient, message, session, cancellationToken);
            }
        }

        private async Task HandleTextUpdateAsync(ITelegramBotClient botClient, Message message, UserSession session, CancellationToken cancellationToken)
        {
            var messageText = message.Text ?? string.Empty;
            var chatId = message.Chat.Id;

            // Command: /start
            if (messageText == "/start")
            {
                session.CurrentState = BotState.WaitingForPassport;
                _sessionService.UpdateSession(session);

                string welcome = "Welcome! I am your AI Insurance Assistant. 🚘\n\n" +
                                 "I will help you issue your policy quickly.\n" +
                                 "To begin, please send a clear photo of your Passport.";

                await botClient.SendMessage(chatId, welcome, cancellationToken: cancellationToken);
                return;
            }

            // Step: Passport Data Confirmation
            if (session.CurrentState == BotState.WaitingForDataConfirmation)
            {
                if (messageText.ToLower() == "yes" || messageText.ToLower() == "да")
                {
                    session.CurrentState = BotState.WaitingForVehicleDocument;
                    _sessionService.UpdateSession(session);
                    await botClient.SendMessage(chatId, "Great! Now please send a photo of your Vehicle Registration document.", cancellationToken: cancellationToken);
                }
                else
                {
                    session.CurrentState = BotState.WaitingForPassport;
                    _sessionService.UpdateSession(session);
                    await botClient.SendMessage(chatId, "Understood. Let's try again. Please send a clearer photo of your passport.", cancellationToken: cancellationToken);
                }
                return;
            }

            // Step: Vehicle Data Confirmation & Final Message Generation
            if (session.CurrentState == BotState.WaitingForPaymentConfirmation)
            {
                if (messageText.ToLower() == "yes" || messageText.ToLower() == "да")
                {
                    await botClient.SendMessage(chatId, "Generating your policy and preparing response... ⏳", cancellationToken: cancellationToken);

                    // Fetch creative AI response from Groq
                    string finalMessage = await _groqService.GenerateFinalMessageAsync();

                    // Reset session state for future applications
                    session.CurrentState = BotState.Start;
                    _sessionService.UpdateSession(session);

                    await botClient.SendMessage(chatId, finalMessage, cancellationToken: cancellationToken);
                }
                else
                {
                    session.CurrentState = BotState.WaitingForVehicleDocument;
                    _sessionService.UpdateSession(session);
                    await botClient.SendMessage(chatId, "Let's try recognizing the vehicle document again. Please upload the photo.", cancellationToken: cancellationToken);
                }
                return;
            }

            await botClient.SendMessage(chatId, $"Current Status: {session.CurrentState}. Please follow the instructions above.", cancellationToken: cancellationToken);
        }

        private async Task HandlePhotoUpdateAsync(ITelegramBotClient botClient, Message message, UserSession session, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var photoId = message.Photo!.Last().FileId;

            // Scenario: Processing Passport Photo
            if (session.CurrentState == BotState.WaitingForPassport)
            {
                await botClient.SendMessage(chatId, "✅ Passport photo received! Processing with Mindee AI... ⏳", cancellationToken: cancellationToken);

                try
                {
                    var fileBytes = await GetFileBytesAsync(botClient, photoId, cancellationToken);
                    var parsedData = await _mindeeService.ParsePassportAsync(fileBytes, "passport.jpg");

                    session.CurrentState = BotState.WaitingForDataConfirmation;
                    _sessionService.UpdateSession(session);

                    await botClient.SendMessage(chatId, $"🎯 Extraction Complete!\n\n{parsedData}\n\nIs this correct? (Reply 'Yes' or 'No')", cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Passport processing error: {Message}", ex.Message);
                    await botClient.SendMessage(chatId, "❌ Error reading document. Please try again with a better photo.", cancellationToken: cancellationToken);
                }
            }
            // Scenario: Processing Vehicle Document
            else if (session.CurrentState == BotState.WaitingForVehicleDocument)
            {
                await botClient.SendMessage(chatId, "✅ Vehicle document received! Analyzing... ⏳", cancellationToken: cancellationToken);

                try
                {
                    var fileBytes = await GetFileBytesAsync(botClient, photoId, cancellationToken);
                    var parsedData = await _mindeeService.ParseVehicleDocumentAsync(fileBytes, "vehicle.jpg");

                    session.CurrentState = BotState.WaitingForPaymentConfirmation;
                    _sessionService.UpdateSession(session);

                    await botClient.SendMessage(chatId, $"🚗 Vehicle Details Extracted:\n\n{parsedData}\n\nIs this correct? If so, I will prepare your policy!", cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Vehicle processing error: {Message}", ex.Message);
                    await botClient.SendMessage(chatId, "❌ Error reading document. Please try again.", cancellationToken: cancellationToken);
                }
            }
        }

        private async Task<byte[]> GetFileBytesAsync(ITelegramBotClient botClient, string fileId, CancellationToken ct)
        {
            var file = await botClient.GetFile(fileId, ct);
            using var ms = new MemoryStream();
            await botClient.DownloadFile(file.FilePath!, ms, ct);
            return ms.ToArray();
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error: [{apiRequestException.ErrorCode}] {apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(errorMessage);
            return Task.CompletedTask;
        }
    }
}