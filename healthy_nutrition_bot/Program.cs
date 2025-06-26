using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using HealthyNutritionBot.service.TokenReader;
using healthy_nutrition_bot.UI;
using HealthyNutritionBot.domain.entities;
using HealthyNutritionBot.domain.repositories;
using HealthyNutritionBot.service;
using HealthyNutritionBot.service.handlers;
using healthy_nutrition_bot.Core.service;

namespace HealthyNutritionBot;

class Program
{
    static async Task Main()
    {
        var supabaseService = new SupabaseService();
        await supabaseService.InitializeAsync();
        var fetchService = new FetchService(supabaseService._supabase);
        var insertService = new InsertService(supabaseService._supabase);

        var tokenReader = new TokenReader();
        string telegramToken = tokenReader.GetTelegramToken();
        string usdaApiKey = tokenReader.GetUsdaApiKey();

        var clarifaiService = new ClarifaiService(tokenReader);
        var httpClient = new HttpClient();
        
        // Create a configuration object with the API key
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"UsdaApiKey", usdaApiKey}
            })
            .Build();
            
        var usdaService = new UsdaService(httpClient, configuration);

        UserRepository userRepository = new UserRepository(supabaseService._supabase);
        StatsOfUsersRepository statsOfUsersRepository = new StatsOfUsersRepository(supabaseService._supabase);
        DailyNormRepository dailyNormRepository = new DailyNormRepository(supabaseService._supabase);

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
            statsOfUsersRepository,
            dailyNormRepository,
            clarifaiService,
            usdaService,
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