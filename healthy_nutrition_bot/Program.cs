using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotNetEnv;
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
using HealthyNutritionBot.Data; // ОБЯЗАТЕЛЬНО: Подключаем пространство имен с AppDbContext

namespace HealthyNutritionBot;

class Program
{
    static async Task Main()
    {
      
        Env.Load();

        var dbContext = new AppDbContext();

        var tokenReader = new TokenReader();
        string telegramToken = tokenReader.GetTelegramToken();
        string usdaApiKey = tokenReader.GetUsdaApiKey();


        var googleVisionService = new GoogleVisionService(tokenReader);
        var httpClient = new HttpClient();

        var usdaService = new UsdaService(usdaApiKey);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        // Передаем dbContext во все репозитории вместо _supabase
        UserRepository userRepository = new UserRepository(dbContext);
        StatsOfUsersRepository statsOfUsersRepository = new StatsOfUsersRepository(dbContext);
        DailyNormRepository dailyNormRepository = new DailyNormRepository(dbContext);
        ProductsRepository productsRepository = new ProductsRepository(dbContext); 

        var botClient = new TelegramBotClient(telegramToken);

        using var cts = new CancellationTokenSource();

        // Инициализируем обработчик сообщений
        MessagesHandler messagesHandler = new MessagesHandler(
            botClient,
            // insertService, <-- УДАЛЕНО! Entity Framework сам сохраняет данные
            userRepository,
            statsOfUsersRepository,
            dailyNormRepository,
            productsRepository,
            googleVisionService,
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