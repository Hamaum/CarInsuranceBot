using Telegram.Bot;
using CarInsuranceBot.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION VALIDATION ---
var config = builder.Configuration;
var botToken = config["BotConfiguration:BotToken"];
var mindeeKey = config["BotConfiguration:MindeeApiKey"];
var groqKey = config["BotConfiguration:GroqApiKey"];

if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(mindeeKey) || string.IsNullOrEmpty(groqKey))
{
    throw new Exception("Critical Configuration Missing: Ensure BotToken, MindeeApiKey, and GroqApiKey are set in appsettings.json or Environment Variables.");
}

// --- 2. SERVICE REGISTRATION (Dependency Injection) ---

// Register Telegram Bot Client
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

// Register Session Management (Singleton to maintain state in memory across requests)
builder.Services.AddSingleton<ISessionService, InMemorySessionService>();

// Register AI & Logic Services
builder.Services.AddSingleton<IMindeeService, MindeeService>();
builder.Services.AddSingleton<IGroqService, GroqService>();

// Register the Telegram Bot Worker as a Background Service
builder.Services.AddHostedService<TelegramBotHostedService>();

builder.Services.AddControllers();

// --- 3. PIPELINE CONFIGURATION ---
var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

Console.WriteLine("--------------------------------------------------");
Console.WriteLine(" Insurance Assistant Bot Service is starting... ");
Console.WriteLine(" Press Ctrl+C to shut down. ");
Console.WriteLine("--------------------------------------------------");

app.Run();