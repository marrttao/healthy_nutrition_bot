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

namespace HealthyNutritionBot.service.handlers
{
    public class HandlerCommands
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IUserRepository _userRepository;
        private readonly IStatsOfUsersRepository _statsOfUsersRepository;
        private readonly string _botToken;
        private readonly ClarifaiService _clarifaiService;
        private readonly UsdaService _usdaService;

        public HandlerCommands(
            ITelegramBotClient botClient,
            IUserRepository userRepository,
            IStatsOfUsersRepository statsOfUsersRepository,
            string botToken,
            ClarifaiService clarifaiService,
            UsdaService usdaService)
        {
            _botClient = botClient;
            _userRepository = userRepository;
            _statsOfUsersRepository = statsOfUsersRepository;
            _botToken = botToken;
            _clarifaiService = clarifaiService;
            _usdaService = usdaService;
        }

        private enum FillStatsStep
        {
            None,
            WaitingHeight,
            WaitingWeight,
            Done
        }

        private readonly Dictionary<long, FillStatsStep> _userSteps = new();
        private readonly Dictionary<long, (int? Height, int? Weight)> _userStats = new();
        private readonly Dictionary<long, bool> _waitingForFoodPhoto = new();

        public async Task HandleStartCommand(long chatId, Message message, CancellationToken cancellationToken)
        {
            // buttons



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
                replyMarkup: Buttons.GetMainMenu(),
                cancellationToken: cancellationToken);

            _userSteps[chatId] = FillStatsStep.WaitingHeight;
            _userStats[chatId] = (null, null);

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
                _userStats[chatId] = (null, null);

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
                    _userStats[chatId] = (height, _userStats[chatId].Weight);
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
                    _userStats[chatId] = (_userStats[chatId].Height, weight);
                    _userSteps[chatId] = FillStatsStep.Done;

                    // Save to database
                    var stats = new StatsOfUsers
                    {
                        TelegramId = chatId,
                        Height = _userStats[chatId].Height ?? 0,
                        Weight = _userStats[chatId].Weight ?? 0,

                        Points = 0 // Set as needed
                    };
                    // if not exists, add it
                    
                    var existingStats = await _statsOfUsersRepository.GetStatsByTelegramIdAsync(chatId);
                    if (existingStats == null)
                    {
                        await _statsOfUsersRepository.AddStatsAsync(stats);
                    }
                    else
                    {
                        existingStats.Height = stats.Height;
                        existingStats.Weight = stats.Weight;
                        await _statsOfUsersRepository.UpdateStatsAsync(existingStats);
                    }
                    
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        $"Thanks! Your stats are saved:\nHeight: {_userStats[chatId].Height} cm\nWeight: {_userStats[chatId].Weight} kg",
                        cancellationToken: cancellationToken);

                    _userSteps[chatId] = FillStatsStep.None;
                    _userStats.Remove(chatId);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "Please enter a valid number for weight.",
                        cancellationToken: cancellationToken);
                }
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
            _waitingForFoodPhoto[chatId] = true;

            await _botClient.SendTextMessageAsync(
                chatId,
                "Send photo of your food.",
                cancellationToken: cancellationToken);
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
        
        // if healthy and food exists
        if (food != null && food.IsHealthy)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                $"✅ This food is healthy! You got 50 points! Calories: {food.Calories}, Protein: {food.Protein}, Fat: {food.Fat}, Carbs: {food.Carbohydrates}",
                cancellationToken: cancellationToken);
            // Update user points
            
            var stats = await _statsOfUsersRepository.GetStatsByTelegramIdAsync(chatId);
            
            if (stats != null)
            {
                stats.Points += 50; // Add points for healthy food
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

        public async Task HandleUserMessage(long chatId, string messageText, CancellationToken cancellationToken)
        {
            if (!_userSteps.ContainsKey(chatId) || _userSteps[chatId] == FillStatsStep.None)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Please start with /start command.",
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