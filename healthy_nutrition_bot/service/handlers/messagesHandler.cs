using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using HealthyNutritionBot.service;
using HealthyNutritionBot.domain.interfaces;

namespace HealthyNutritionBot.service.handlers;

public class MessagesHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly HandlerCommands _commandHandler;

    public MessagesHandler(ITelegramBotClient botClient, InsertService insertService, IUserRepository userRepository)
    {
        _botClient = botClient;
        _commandHandler = new HandlerCommands(botClient, insertService, userRepository);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text == null)
            return;

        var chatId = update.Message.Chat.Id;
        var messageText = update.Message.Text;

        Console.WriteLine($"Получено сообщение от {chatId}: {messageText}");

        if (messageText == "/start")
        {
            await _commandHandler.HandleStartCommand(chatId, update.Message, cancellationToken);
        }
        else
        {
            await _commandHandler.HandleUnknownCommand(chatId, cancellationToken);
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
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