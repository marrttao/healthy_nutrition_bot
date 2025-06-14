using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using HealthyNutritionBot.service.TokenReader;
using healthy_nutrition_bot.UI; // Add this line

namespace HealthyNutritionBot;

class Program
{
    static async Task Main()
    {
        var tokenReader = new TokenReader();
        string telegramToken = tokenReader.GetTelegramToken();
        var botClient = new TelegramBotClient(telegramToken);

        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Бот {me.Username} запущен.");

        await Task.Delay(-1);
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text == null)
            return;

        var chatId = update.Message.Chat.Id;
        var messageText = update.Message.Text;

        Console.WriteLine($"Получено сообщение от {chatId}: {messageText}");

        if (messageText == "/start")
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Привет! Я бот по здоровому питанию.",
                replyMarkup: Buttons.GetMainMenu(), // Show main menu buttons
                cancellationToken: cancellationToken
            );
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Пока я понимаю только /start. В будущем научусь большему :)",
                cancellationToken: cancellationToken
            );
        }
    }

    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}

