using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
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
        var clarifaiService = new ClarifaiService(new TokenReader());

        UserRepository userRepository = new UserRepository(supabaseService._supabase);

        var user = new User
        {
            TelegramId = 12345,
            Name = "John",
            Lastname = "Doe",
            IsActive = true
        };
        await insertService.InsertAsync(user);

        var retrievedUsers = await fetchService.GetDataByConditionAsync<User>(
            "users",
            u => u.TelegramId == user.TelegramId
        );
        var retrievedUser = retrievedUsers.FirstOrDefault();
        Console.WriteLine(retrievedUser != null
            ? $"Retrieved User: ID={retrievedUser.id}, TelegramID={retrievedUser.TelegramId}, Name={retrievedUser.Name}, Lastname={retrievedUser.Lastname}, IsActive={retrievedUser.IsActive}"
            : "User not found");

        var tokenReader = new TokenReader();
        string telegramToken = tokenReader.GetTelegramToken();
        var botClient = new TelegramBotClient(telegramToken);

        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        MessagesHandler messagesHandler = new MessagesHandler(
            botClient,
            insertService,
            userRepository,
            clarifaiService,
            telegramToken
        );

        botClient.StartReceiving(
            updateHandler: messagesHandler.HandleUpdateAsync,
            pollingErrorHandler: messagesHandler.HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Бот {me.Username} запущен.");

        await Task.Delay(Timeout.Infinite, cts.Token);
    }
}