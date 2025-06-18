using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using HealthyNutritionBot.service.TokenReader;
using healthy_nutrition_bot.UI;
using HealthyNutritionBot.domain.entities;
using HealthyNutritionBot.domain.repositories;
using HealthyNutritionBot.service;
using HealthyNutritionBot.service.handlers;

namespace HealthyNutritionBot;

class Program
{
    static async Task Main()
    {
        var supabaseService = new SupabaseService();
        await supabaseService.InitializeAsync();
        var fetchService = new FetchService(supabaseService._supabase);
        var insertService = new InsertService(supabaseService._supabase);

        UserRepository userRepository = new UserRepository(supabaseService._supabase);

        HealthyNutritionBot.domain.entities.User user = new HealthyNutritionBot.domain.entities.User
        {
            TelegramId = 12345,
            Name = "John",
            Lastname = "Doe",
            IsActive = true
        };
        // Insert a new user
        await insertService.InsertUserAsync(user);
        var retrievedUsers = await fetchService.GetDataByConditionAsync<HealthyNutritionBot.domain.entities.User>(
            "users",
            u => u.TelegramId == user.TelegramId
        );
        var retrievedUser = retrievedUsers.FirstOrDefault();
        if (retrievedUser != null)
        {
            Console.WriteLine($"Retrieved User: ID={retrievedUser.id}, TelegramID={retrievedUser.TelegramId}, " +
                              $"Name={retrievedUser.Name}, Lastname={retrievedUser.Lastname}, IsActive={retrievedUser.IsActive}");
        }
        else
        {
            Console.WriteLine("User not found");
        }
        


        // Telegram bot initialization
        var tokenReader = new TokenReader();
        string telegramToken = tokenReader.GetTelegramToken();
        var botClient = new TelegramBotClient(telegramToken);

        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };
        MessagesHandler messagesHandler = new MessagesHandler(botClient, insertService, userRepository);

        botClient.StartReceiving(
            updateHandler: messagesHandler.HandleUpdateAsync,
            pollingErrorHandler: messagesHandler.HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Бот {me.Username} запущен.");

        await Task.Delay(-1);
    }
}