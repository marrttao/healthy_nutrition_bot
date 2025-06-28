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
using Quartz;
using Quartz.Impl;
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

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("tokens.json", optional: false, reloadOnChange: true)
            .Build();
        var usdaService = new UsdaService(usdaApiKey);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        UserRepository userRepository = new UserRepository(supabaseService._supabase);
        StatsOfUsersRepository statsOfUsersRepository = new StatsOfUsersRepository(supabaseService._supabase);
        DailyNormRepository dailyNormRepository = new DailyNormRepository(supabaseService._supabase);
        ProductsRepository productsRepository = new ProductsRepository(supabaseService._supabase); // <-- added

        var botClient = new TelegramBotClient(telegramToken);

        using var cts = new CancellationTokenSource();

        MessagesHandler messagesHandler = new MessagesHandler(
            botClient,
            insertService,
            userRepository,
            statsOfUsersRepository,
            dailyNormRepository,
            productsRepository, // <-- added
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
        StdSchedulerFactory factory = new StdSchedulerFactory();
        IScheduler scheduler = await factory.GetScheduler();
        await scheduler.Start();

        // Планирование сброса таблицы в 00:00 по EEST
        IJobDetail resetJob = JobBuilder.Create<ResetDailyGoals>()
            .WithIdentity("resetJob", "group1")
            .Build();

        ITrigger resetTrigger = TriggerBuilder.Create()
            .WithIdentity("resetTrigger", "group1")
            .WithCronSchedule("0 0 0 * * ?", x => x
                .InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Kiev")))
            .Build();
        await scheduler.ScheduleJob(resetJob, resetTrigger);

        Console.WriteLine("Планировщик запущен. Таблица будет очищаться в 00:00 EEST...");

        // Держим приложение запущенным
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
}