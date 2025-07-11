using Telegram.Bot;
using Telegram.Bot.Types;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using healthy_nutrition_bot.UI;
using HealthyNutritionBot.domain.entities;
using UserEntity = HealthyNutritionBot.domain.entities.User;
using HealthyNutritionBot.domain.interfaces;
using HealthyNutritionBot.service;
using HealthyNutritionBot.service.TokenReader;
using healthy_nutrition_bot.Core.service;
using healthy_nutrition_bot.domain.entities;
using HealthyNutritionBot.domain.utils;

namespace HealthyNutritionBot.service.handlers
{
    public class HandlerCommands
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IUserRepository _userRepository;
        private readonly IStatsOfUsersRepository _statsOfUsersRepository;
        private readonly IDailyNormRepository _dailyNormRepository;
        private readonly IProductsRepository _ProductsRepository;
        private readonly string _botToken;
        private readonly ClarifaiService _clarifaiService;
        private readonly UsdaService _usdaService;
        

        public HandlerCommands(
            ITelegramBotClient botClient,
            IUserRepository userRepository,
            IStatsOfUsersRepository statsOfUsersRepository,
            IDailyNormRepository dailyNormRepository,
            IProductsRepository productsRepository, // <-- add this
            string botToken,
            ClarifaiService clarifaiService,
            UsdaService usdaService)
        {
            _botClient = botClient;
            _userRepository = userRepository;
            _statsOfUsersRepository = statsOfUsersRepository;
            _dailyNormRepository = dailyNormRepository;
            _ProductsRepository = productsRepository; // <-- fix assignment
            _botToken = botToken;
            _clarifaiService = clarifaiService;
            _usdaService = usdaService;
        }

        private enum FillStatsStep
        {
            None,
            WaitingHeight,
            WaitingWeight,
            WaitingGender,
            WaitingGoal,
            WaitingActivity,
            Done
        }

// Update the user stats dictionary to include new fields
        

        private readonly Dictionary<long, FillStatsStep> _userSteps = new();
        private readonly Dictionary<long, (int? Height, int? Weight, string Gender, string Goal, string Activity)> _userStats = new();
        private readonly Dictionary<long, bool> _waitingForFoodPhoto = new();

        public async Task HandleStartCommand(long chatId, Message message, CancellationToken cancellationToken)
        {
            var user = new UserEntity
            {
                TelegramId = chatId,
                Name = message?.From?.FirstName ?? "User",
                Lastname = message?.From?.LastName ?? "",
                IsActive = true
            };

            var existingUser = await _userRepository.GetUserById(chatId);
            if (existingUser != null)
            {
                Console.WriteLine($"User {existingUser.Name} already exists.");
            }
            else
            {
                await _userRepository.AddUserAsync(user);
                Console.WriteLine($"Added user {user.Name}.");
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                "Hi there! I'm your personal nutrition bot. Let's start by filling up your stats.",
                cancellationToken: cancellationToken);

            _userSteps[chatId] = FillStatsStep.WaitingHeight;
            _userStats[chatId] = (null, null, null, null, null);

            await _botClient.SendTextMessageAsync(
                chatId,
                "Enter your height in centimeters (cm):",
                cancellationToken: cancellationToken);
        }

        public async Task HandleChangeOwnStatsCommand(long chatId, CancellationToken cancellationToken)
        {
            if (!_userSteps.ContainsKey(chatId) || _userSteps[chatId] == FillStatsStep.None)
            {
                _userSteps[chatId] = FillStatsStep.WaitingHeight;
                _userStats[chatId] = (null, null, null, null, null);

                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Let's update your stats. Please enter your height in centimeters (cm):",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "You are already in the process of updating your stats. Please continue.",
                    cancellationToken: cancellationToken);
            }
        }

        public async Task HandlerFillStats(long chatId, string messageText, CancellationToken cancellationToken)
{
    var step = _userSteps[chatId];

    if (step == FillStatsStep.WaitingHeight)
    {
        if (int.TryParse(messageText, out int height))
        {
            _userStats[chatId] = (height, _userStats[chatId].Weight, _userStats[chatId].Gender,
                                _userStats[chatId].Goal, _userStats[chatId].Activity);
            _userSteps[chatId] = FillStatsStep.WaitingWeight;

            await _botClient.SendTextMessageAsync(
                chatId,
                "Enter your weight in kilograms (kg):",
                cancellationToken: cancellationToken);
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "Please enter a valid number for height.",
                cancellationToken: cancellationToken);
        }
        return;
    }

    if (step == FillStatsStep.WaitingWeight)
    {
        if (int.TryParse(messageText, out int weight))
        {
            _userStats[chatId] = (_userStats[chatId].Height, weight, _userStats[chatId].Gender,
                                _userStats[chatId].Goal, _userStats[chatId].Activity);
            _userSteps[chatId] = FillStatsStep.WaitingGender;

            await _botClient.SendTextMessageAsync(
                chatId,
                "Select your gender:",
                replyMarkup: Buttons.GetGenderButtons(),
                cancellationToken: cancellationToken);
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "Please enter a valid number for weight.",
                cancellationToken: cancellationToken);
        }
        return;
    }

    if (step == FillStatsStep.WaitingGender)
    {
        _userStats[chatId] = (_userStats[chatId].Height, _userStats[chatId].Weight,
                            messageText, _userStats[chatId].Goal, _userStats[chatId].Activity);
        _userSteps[chatId] = FillStatsStep.WaitingGoal;

        await _botClient.SendTextMessageAsync(
            chatId,
            "Select your goal:",
            replyMarkup: Buttons.GetGoalButtons(),
            cancellationToken: cancellationToken);
        return;
    }

    if (step == FillStatsStep.WaitingGoal)
    {
        _userStats[chatId] = (_userStats[chatId].Height, _userStats[chatId].Weight,
                            _userStats[chatId].Gender, messageText, _userStats[chatId].Activity);
        _userSteps[chatId] = FillStatsStep.WaitingActivity;

        await _botClient.SendTextMessageAsync(
            chatId,
            "Select your activity level:",
            replyMarkup: Buttons.GetActivityButtons(),
            cancellationToken: cancellationToken);
        return;
    }

    if (step == FillStatsStep.WaitingActivity)
    {
        _userStats[chatId] = (_userStats[chatId].Height, _userStats[chatId].Weight,
                            _userStats[chatId].Gender, _userStats[chatId].Goal, messageText);
        _userSteps[chatId] = FillStatsStep.Done;

        // Save to database
        var height = _userStats[chatId].Height ?? 0;
        var weight = _userStats[chatId].Weight ?? 0;

        var stats = new StatsOfUsers
        {
            TelegramId = chatId,
            Height = height,
            Weight = weight,
            Gender = _userStats[chatId].Gender,
            Goal = _userStats[chatId].Goal,
            Activity = _userStats[chatId].Activity,
            Points = 0
        };

        var existingStats = await _statsOfUsersRepository.GetStatsByTelegramIdAsync(chatId);
        if (existingStats == null)
        {
            await _statsOfUsersRepository.AddStatsAsync(stats);
        }
        else
        {
            existingStats.Height = stats.Height;
            existingStats.Weight = stats.Weight;
            existingStats.Gender = stats.Gender;
            existingStats.Goal = stats.Goal;
            existingStats.Activity = stats.Activity;
            await _statsOfUsersRepository.UpdateStatsAsync(existingStats);
        }

        var calculator = new NutritionCalculator(
            _userStats[chatId].Height ?? 0,
            _userStats[chatId].Weight ?? 0,
            _userStats[chatId].Gender,
            _userStats[chatId].Goal,
            _userStats[chatId].Activity,
            _dailyNormRepository 
        );
        await calculator.GetNutritionReportAsync(chatId);

        await _botClient.SendTextMessageAsync(
            chatId,
            $"Thanks! Your stats are saved:\nHeight: {_userStats[chatId].Height} cm\nWeight: {_userStats[chatId].Weight} kg\nGender: {_userStats[chatId].Gender}\nGoal: {_userStats[chatId].Goal}\nActivity Level: {_userStats[chatId].Activity}",
            replyMarkup: Buttons.GetMainMenu(),
            cancellationToken: cancellationToken);

        _userSteps[chatId] = FillStatsStep.None;
        _userStats.Remove(chatId);
    }
}
        

        public async Task HandleSettingsCommand(long chatId, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "Settings menu",
                replyMarkup: Buttons.Settings(),
                cancellationToken: cancellationToken);
        }

        public async Task HandleBackCommand(long chatId, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "You are back to the main menu.",
                replyMarkup: Buttons.GetMainMenu(),
                cancellationToken: cancellationToken);
        }

        public async Task HandleEatCommand(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                // back button
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Starting eat 🍽️",
                    replyMarkup: Buttons.Back(),
                    cancellationToken: cancellationToken);

                var norm = await _dailyNormRepository.GetDailyNorm(chatId);

                if (norm == null)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Failed to get your daily norm. Try again later.",
                        cancellationToken: cancellationToken);
                    return;
                }

                if (norm.CaloriesToday >= norm.Calories)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "⚠️ You shouldn't eat more today. You've reached your calorie limit.",
                        cancellationToken: cancellationToken);
                    return;
                }

                _waitingForFoodPhoto[chatId] = true;

                await _botClient.SendTextMessageAsync(
                    chatId,
                    "📸 Please send a photo of your food.",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                // Логирование ошибки (если есть логгер)
                Console.WriteLine($"[HandleEatCommand] Error: {ex.Message}");

                await _botClient.SendTextMessageAsync(
                    chatId,
                    "❌ An error occurred while processing your request. Please try again later.",
                    cancellationToken: cancellationToken);
            }
        }

        public async Task HandlePhotoAsync(Message message, CancellationToken cancellationToken)
{
    long chatId = message.Chat.Id;
    if (_waitingForFoodPhoto.TryGetValue(chatId, out bool waiting) && waiting)
    {
        _waitingForFoodPhoto[chatId] = false;

        var photo = message.Photo[^1];
        var file = await _botClient.GetFileAsync(photo.FileId);
        var filePath = file.FilePath;
        var fileUrl = $"https://api.telegram.org/file/bot{_botToken}/{filePath}";

        var result = await _clarifaiService.RecognizeFoodAsync(fileUrl);
        await _botClient.SendTextMessageAsync(chatId, $"🍽 I see: {result}", cancellationToken: cancellationToken);
        
        // Get the list of food info and take the first item if available
        var foodList = await _usdaService.GetFoodInfoAsync(result);
        var food = foodList.FirstOrDefault();
        
        // Send food details to the user
        if (food == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "Sorry, I could not recognize the food or retrieve its nutritional information.",
                cancellationToken: cancellationToken);
            return;
        }

        await _botClient.SendTextMessageAsync(
            chatId,
            $"Calories: {food.Calories}, Protein: {food.Protein}, Fat: {food.Fat}, Carbs: {food.Carbohydrates}",
            cancellationToken: cancellationToken);

        var existingNorm = await _dailyNormRepository.GetDailyNorm(chatId);

        if (existingNorm == null)
        {
            var newNorm = new DailyNorm
            {
                TelegramId = chatId,
                CaloriesToday = food.Calories,
                ProteinsToday = food.Protein,
                FatsToday = food.Fat,
                CarbsToday = food.Carbohydrates
            };
            await _dailyNormRepository.AddDailyNorm(newNorm);
        }
        else
        {
            existingNorm.CaloriesToday += food.Calories;
            existingNorm.ProteinsToday += food.Protein;
            existingNorm.FatsToday += food.Fat;
            existingNorm.CarbsToday += food.Carbohydrates;
            await _dailyNormRepository.UpdateDailyNorm(existingNorm);
        }

        if (food != null && food.IsHealthy)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                $"✅ This food is healthy! You got 50 points! Calories: {food.Calories}, Protein: {food.Protein}, Fat: {food.Fat}, Carbs: {food.Carbohydrates}",
                cancellationToken: cancellationToken);

            var stats = await _statsOfUsersRepository.GetStatsByTelegramIdAsync(chatId);

            if (stats != null)
            {
                stats.Points += 50;
                stats.ShopPoints += 50;
                await _statsOfUsersRepository.UpdateStatsAsync(stats);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "⚠️ Your stats are not found. Please fill your stats first using /start command.",
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "❌ This food is not healthy or not recognized.",
                cancellationToken: cancellationToken);
        }
    }
}
        
        public async Task HandleHelpCommand(long chatId, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "Here are the commands you can use:\n" +
                "/start - Start the bot and fill your stats\n" +
                "/change_stats - Change your stats\n" +
                "/daily_goal - Show your daily goals\n" +
                "/eat - Log your food intake\n" +
                "/shop - View the shop\n" +
                "/stats - View your stats\n" +
                "/settings - Change settings\n" +
                "/back - Go back to the main menu",
                cancellationToken: cancellationToken);
        }
        public async Task HandleDailyGoalCommand(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _statsOfUsersRepository.GetStatsByTelegramIdAsync(chatId);
            if (stats == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Please fill your stats first using /start command.",
                    cancellationToken: cancellationToken);
                return;
            }

            var dailyNorm = await _dailyNormRepository.GetDailyNorm(chatId);
    
            if (dailyNorm == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Your daily goals have not been set yet.",
                    cancellationToken: cancellationToken);
                return;
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                $"Your daily goal:\n" +
                $"Calories: {dailyNorm.CaloriesToday} kcal / {dailyNorm.Calories} kcal \n" +
                $"Protein: {dailyNorm.ProteinsToday} g / {dailyNorm.Proteins} \n" +
                $"Fat: {dailyNorm.FatsToday} g / {dailyNorm.Fats} g \n" +
                $"Carbohydrates: {dailyNorm.CarbsToday} g / {dailyNorm.Carbs} g",
                cancellationToken: cancellationToken);
        }

        public async Task HandleShopCommand(long chatId, CancellationToken cancellationToken)
        {
            var shopService = new ShopService(_ProductsRepository);
            await shopService.ShowShopAsync(_botClient, chatId);

        }

        public async Task HandleStatsCommand(long chatId, CancellationToken cancellationToken)
        {
            var stats = await _statsOfUsersRepository.GetStatsByTelegramIdAsync(chatId);
            if (stats == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Please fill your stats first using ",
                    cancellationToken: cancellationToken);
                return;
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                $"Your stats:\n" +
                $"Height: {stats.Height} cm\n" +
                $"Weight: {stats.Weight} kg\n" +
                $"Gender: {stats.Gender} \n" +
                $"Goal: {stats.Goal} \n" +
                $"Activity Level: {stats.Activity} \n" +
                $"Points: {stats.Points} \n" +
                $"Shop Points: {stats.ShopPoints} \n",
                cancellationToken: cancellationToken);
        }
        
        

        
        public async Task HandleUserMessage(long chatId, string messageText, CancellationToken cancellationToken)
        {
            if (!_userSteps.ContainsKey(chatId) || _userSteps[chatId] == FillStatsStep.None)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "I havent understood",
                    cancellationToken: cancellationToken);
                return;
            }

            await HandlerFillStats(chatId, messageText, cancellationToken);
        }

        public async Task HandleUnknownCommand(long chatId, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "I only understand /start for now. More to come!",
                cancellationToken: cancellationToken);
        }
    }
}