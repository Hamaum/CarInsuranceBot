using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using CarInsuranceBot.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CarInsuranceBot.Services
{
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
                AllowedUpdates = Array.Empty<UpdateType>()
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

            if (message.Type == MessageType.Text)
            {
                await HandleTextUpdateAsync(botClient, message, session, cancellationToken);
            }
            else if (message.Type == MessageType.Photo)
            {
                await HandlePhotoUpdateAsync(botClient, message, session, cancellationToken);
            }
        }

        private async Task HandleTextUpdateAsync(ITelegramBotClient botClient, Message message, UserSession session, CancellationToken cancellationToken)
        {
            var messageText = message.Text ?? string.Empty;
            var chatId = message.Chat.Id;

            // 1. Команда /start
            if (messageText == "/start")
            {
                session.CurrentState = BotState.WaitingForPassport;
                _sessionService.UpdateSession(session);

                await botClient.SendMessage(chatId, "Thinking... ⏳", cancellationToken: cancellationToken);
                string prompt = "Introduce yourself as a polite AI Car Insurance Assistant. Ask the user to begin by sending a clear photo of their Passport.";
                string welcome = await _groqService.GenerateBotResponseAsync(prompt);

                await botClient.SendMessage(chatId, welcome, cancellationToken: cancellationToken);
                return;
            }

            // 2. Подтверждение данных паспорта
            if (session.CurrentState == BotState.WaitingForDataConfirmation)
            {
                await botClient.SendMessage(chatId, "Thinking... ⏳", cancellationToken: cancellationToken);
                if (messageText.ToLower() == "yes" || messageText.ToLower() == "да" || messageText.ToLower() == "y")
                {
                    session.CurrentState = BotState.WaitingForVehicleDocument;
                    _sessionService.UpdateSession(session);

                    string prompt = "The user confirmed their passport data is correct. Thank them, and now ask them to send a clear photo of their Vehicle Registration document.";
                    string aiResponse = await _groqService.GenerateBotResponseAsync(prompt);
                    await botClient.SendMessage(chatId, aiResponse, cancellationToken: cancellationToken);
                }
                else
                {
                    session.CurrentState = BotState.WaitingForPassport;
                    _sessionService.UpdateSession(session);

                    string prompt = "The user indicated the extracted passport data was incorrect. Apologize and politely ask them to retake and resubmit a clearer photo of their passport.";
                    string aiResponse = await _groqService.GenerateBotResponseAsync(prompt);
                    await botClient.SendMessage(chatId, aiResponse, cancellationToken: cancellationToken);
                }
                return;
            }

            // 3. Подтверждение данных техпаспорта (Пункт 5 из ТЗ)
            if (session.CurrentState == BotState.WaitingForPaymentConfirmation)
            {
                await botClient.SendMessage(chatId, "Thinking... ⏳", cancellationToken: cancellationToken);
                if (messageText.ToLower() == "yes" || messageText.ToLower() == "да" || messageText.ToLower() == "y")
                {
                    session.CurrentState = BotState.WaitingForPriceConfirmation;
                    _sessionService.UpdateSession(session);

                    string prompt = "The user confirmed their vehicle data. Inform the user that the fixed price for the car insurance is exactly 100 USD. Ask them if they agree with this price by replying 'Yes' or 'No'.";
                    string aiResponse = await _groqService.GenerateBotResponseAsync(prompt);
                    await botClient.SendMessage(chatId, aiResponse, cancellationToken: cancellationToken);
                }
                else
                {
                    session.CurrentState = BotState.WaitingForVehicleDocument;
                    _sessionService.UpdateSession(session);

                    string prompt = "The user indicated the extracted vehicle data was incorrect. Apologize and politely ask them to retake and resubmit a clearer photo of their vehicle document.";
                    string aiResponse = await _groqService.GenerateBotResponseAsync(prompt);
                    await botClient.SendMessage(chatId, aiResponse, cancellationToken: cancellationToken);
                }
                return;
            }

            // 4. Подтверждение цены 100 USD и генерация полиса
            if (session.CurrentState == BotState.WaitingForPriceConfirmation)
            {
                await botClient.SendMessage(chatId, "Generating your policy and preparing response... ⏳", cancellationToken: cancellationToken);
                if (messageText.ToLower() == "yes" || messageText.ToLower() == "да" || messageText.ToLower() == "y")
                {
                    // Пункт 6 из ТЗ: Финальный полис
                    string finalMessage = await _groqService.GenerateFinalMessageAsync();

                    session.CurrentState = BotState.Start;
                    _sessionService.UpdateSession(session);

                    await botClient.SendMessage(chatId, finalMessage, cancellationToken: cancellationToken);
                }
                else
                {
                    string prompt = "The user disagreed with the price. Apologize politely and explain that 100 USD is the only available fixed price for this insurance policy. Ask if they agree to proceed with 100 USD by replying 'Yes'.";
                    string aiResponse = await _groqService.GenerateBotResponseAsync(prompt);
                    await botClient.SendMessage(chatId, aiResponse, cancellationToken: cancellationToken);
                }
                return;
            }

            // Если бот не понял команду
            string fallbackPrompt = "The user sent an unrecognized command. Politely ask them to follow the current instructions or type /start to restart the process.";
            string fallbackResponse = await _groqService.GenerateBotResponseAsync(fallbackPrompt);
            await botClient.SendMessage(chatId, fallbackResponse, cancellationToken: cancellationToken);
        }

        private async Task HandlePhotoUpdateAsync(ITelegramBotClient botClient, Message message, UserSession session, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var photoId = message.Photo!.Last().FileId;

            // Обработка Паспорта
            if (session.CurrentState == BotState.WaitingForPassport)
            {
                await botClient.SendMessage(chatId, "✅ Passport photo received! Processing with Mindee AI... ⏳", cancellationToken: cancellationToken);

                try
                {
                    var fileBytes = await GetFileBytesAsync(botClient, photoId, cancellationToken);
                    var parsedData = await _mindeeService.ParsePassportAsync(fileBytes, "passport.jpg");

                    session.CurrentState = BotState.WaitingForDataConfirmation;
                    _sessionService.UpdateSession(session);

                    await botClient.SendMessage(chatId, "Thinking... ⏳", cancellationToken: cancellationToken);
                    string prompt = $"Tell the user that you successfully extracted their passport data:\n{parsedData}\nPolitely ask them to confirm if this information is exactly correct by replying 'Yes' or 'No'.";
                    string aiResponse = await _groqService.GenerateBotResponseAsync(prompt);

                    await botClient.SendMessage(chatId, aiResponse, cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Passport processing error: {Message}", ex.Message);
                    await botClient.SendMessage(chatId, "❌ Error reading document. Please try again with a better photo.", cancellationToken: cancellationToken);
                }
            }
            // Обработка Техпаспорта
            else if (session.CurrentState == BotState.WaitingForVehicleDocument)
            {
                await botClient.SendMessage(chatId, "✅ Vehicle document received! Analyzing... ⏳", cancellationToken: cancellationToken);

                try
                {
                    var fileBytes = await GetFileBytesAsync(botClient, photoId, cancellationToken);
                    var parsedData = await _mindeeService.ParseVehicleDocumentAsync(fileBytes, "vehicle.jpg");

                    session.CurrentState = BotState.WaitingForPaymentConfirmation;
                    _sessionService.UpdateSession(session);

                    await botClient.SendMessage(chatId, "Thinking... ⏳", cancellationToken: cancellationToken);
                    string prompt = $"Tell the user that you successfully extracted their vehicle data:\n{parsedData}\nPolitely ask them to confirm if this information is exactly correct by replying 'Yes' or 'No'.";
                    string aiResponse = await _groqService.GenerateBotResponseAsync(prompt);

                    await botClient.SendMessage(chatId, aiResponse, cancellationToken: cancellationToken);
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