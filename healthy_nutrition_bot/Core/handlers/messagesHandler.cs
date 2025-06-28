using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using healthy_nutrition_bot.Core.service;
using HealthyNutritionBot.service;
using HealthyNutritionBot.domain.interfaces;

namespace HealthyNutritionBot.service.handlers;

public class MessagesHandler
{
    private ITelegramBotClient _botClient;
    private readonly HandlerCommands _commandHandler;

    public void SetBotClient(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }
    public MessagesHandler(
        ITelegramBotClient botClient,
        InsertService insertService,
        IUserRepository userRepository,
        IStatsOfUsersRepository statsOfUsersRepository,
        IDailyNormRepository dailyNormRepository,
        IProductsRepository productsRepository, // <-- added parameter
        ClarifaiService clarifaiService,
        UsdaService usdaService,
        string botToken)
    {
        _botClient = botClient;
        _commandHandler = new HandlerCommands(botClient, userRepository, statsOfUsersRepository, dailyNormRepository, productsRepository, botToken, clarifaiService, usdaService);
    }

    public async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message == null)
            return;

        var message = update.Message;
        var chatId = message.Chat.Id;

        if (message.Photo != null)
        {
            Console.WriteLine($"Получено фото от {chatId}");
            await _commandHandler.HandlePhotoAsync(message, cancellationToken);
            return;
        }

        if (message.Text == null)
            return;

        var messageText = message.Text;

        Console.WriteLine($"Получено сообщение от {chatId}: {messageText}");

        if (messageText == "/start")
            await _commandHandler.HandleStartCommand(chatId, message, cancellationToken);
        else if (messageText == "Eat")
            await _commandHandler.HandleEatCommand(chatId, cancellationToken);
        else if (messageText == "Settings")
            await _commandHandler.HandleSettingsCommand(chatId, cancellationToken);
        else if (messageText == "Change own stats")
            await _commandHandler.HandleChangeOwnStatsCommand(chatId, cancellationToken);
        else if (messageText == "Shop")
            await _commandHandler.HandleShopCommand(chatId, cancellationToken);
        else if (messageText == "Stats") 
            await _commandHandler.HandleStatsCommand(chatId, cancellationToken);
        else if (messageText == "Daily Goal")
            await _commandHandler.HandleDailyGoalCommand(chatId, cancellationToken);
        else if (messageText == "Back")
            await _commandHandler.HandleBackCommand(chatId, cancellationToken);
        else
            await _commandHandler.HandleUserMessage(chatId, messageText, cancellationToken);
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken)
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