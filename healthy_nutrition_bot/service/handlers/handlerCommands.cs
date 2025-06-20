// handlerCommands.cs
using Telegram.Bot;
using Telegram.Bot.Types;
using System;
using System.Threading;
using System.Threading.Tasks;
using healthy_nutrition_bot.UI;
using HealthyNutritionBot.domain.entities;
using HealthyNutritionBot.domain.interfaces;

namespace HealthyNutritionBot.service.handlers;

public class HandlerCommands
{
    private readonly ITelegramBotClient _botClient;
    private readonly InsertService _insertService;
    private readonly IUserRepository _userRepository;

    public HandlerCommands(ITelegramBotClient botClient, InsertService insertService, IUserRepository userRepository)
    {
        _botClient = botClient;
        _insertService = insertService;
        _userRepository = userRepository;
    }

    public async Task HandleStartCommand(long chatId, Message message, CancellationToken cancellationToken)
    {
        // Create and insert user
        var user = new HealthyNutritionBot.domain.entities.User
        {
            TelegramId = chatId,
            Name = message?.From?.FirstName ?? "User",
            Lastname = message?.From?.LastName ?? "",
            IsActive = true
        };
        // if not exists, insert user
        var existingUser = await _userRepository.GetUserById(chatId);
        if (existingUser != null)
        {
            Console.WriteLine($"Пользователь {existingUser.Name.ToString()} {existingUser.Lastname.ToString()} с ID {existingUser.TelegramId.ToString()} уже существует.");
            return;
        }
        else
        {
            await _userRepository.AddUserAsync(user);
        }

        Console.WriteLine($"Пользователь {user.Name.ToString()} {user.Lastname.ToString()} с ID {user.TelegramId.ToString()} добавлен в базу данных.");

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Hi there! I'm your personal nutrition bot. I can help you with healthy eating habits and recipes. " +
                  "For start lets fill up your stats",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
            replyMarkup: Buttons.GetMainMenu(),
            cancellationToken: cancellationToken
        );
    }

    public async Task HandlerFillStats(long chatId, Message message, CancellationToken cancellationToken)
    {
        // Send message asking for height
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Enter your height in centimeters (cm):",
            cancellationToken: cancellationToken
        );

        // Check if message contains valid height
        while (true)
        {
            if (message.Text != null && int.TryParse(message.Text, out int height))
            {
                int heightt = height;
                // TODO: Save height to database
                Console.WriteLine($"User height: {height} cm");
                break;
            }   
            else

            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "You entered an invalid height. Please enter valid height.",
                    cancellationToken: cancellationToken
                );
            }
        }

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Enter your weight in killograms (kg):",
            cancellationToken: cancellationToken
            );
        // Check if message contains valid weight
        
        while (true)
        {
            if (message.Text != null && int.TryParse(message.Text, out int weight))
            {
                int weightt = weight;
                Console.WriteLine($"User height: {weight} kg");
                break;
            }
            


        }
    public async Task HandleUnknownCommand(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Пока я понимаю только /start. В будущем научусь большему :)",
            cancellationToken: cancellationToken
        );
    }
}