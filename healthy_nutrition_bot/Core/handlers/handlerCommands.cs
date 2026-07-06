using Telegram.Bot;
using Telegram.Bot.Types;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
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
        private readonly GoogleVisionService _googleVisionService;
        private readonly UsdaService _usdaService;

        public HandlerCommands(
            ITelegramBotClient botClient,
            IUserRepository userRepository,
            IStatsOfUsersRepository statsOfUsersRepository,
            IDailyNormRepository dailyNormRepository,
            IProductsRepository productsRepository,
            string botToken,
            GoogleVisionService googleVisionService,
            UsdaService usdaService)
        {
            _botClient = botClient;
            _userRepository = userRepository;
            _statsOfUsersRepository = statsOfUsersRepository;
            _dailyNormRepository = dailyNormRepository;
            _ProductsRepository = productsRepository;
            _botToken = botToken;
            _googleVisionService = googleVisionService;
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

        // Значения синхронизированы с текстами кнопок из Buttons.cs
        // (GetGenderButtons / GetGoalButtons / GetActivityButtons).
        private static readonly string[] ValidGenders = { "Male", "Female" };
        private static readonly string[] ValidGoals = { "Lose", "Maintain", "Gain" };
        private static readonly string[] ValidActivities = { "Low", "Medium", "High" };

        private const int MinHeightCm = 100;
        private const int MaxHeightCm = 250;
        private const int MinWeightKg = 20;
        private const int MaxWeightKg = 300;

        private static readonly string[] CancelWords = { "cancel", "back", "отмена", "назад" };

        private readonly Dictionary<long, FillStatsStep> _userSteps = new();
        private readonly Dictionary<long, (int? Height, int? Weight, string Gender, string Goal, string Activity)> _userStats = new();
        private readonly Dictionary<long, bool> _waitingForFoodPhoto = new();

        // ---------- Вспомогательные проверки состояния ----------

        private bool IsRegistrationInProgress(long chatId) =>
            _userSteps.TryGetValue(chatId, out var step) && step != FillStatsStep.None && step != FillStatsStep.Done;

        private async Task<bool> IsUserRegisteredAsync(long chatId)
        {
            var stats = await _statsOfUsersRepository.GetStatsByTelegramIdAsync(chatId);
            return stats != null;
        }

        /// <summary>
        /// Единая точка входа для всех команд, кроме /start и заполнения анкеты.
        /// Возвращает true, если можно продолжать выполнение команды.
        /// Если false — сообщение пользователю уже отправлено, дальше делать ничего не нужно.
        /// </summary>
        private async Task<bool> EnsureCanUseFeatureAsync(long chatId, CancellationToken cancellationToken)
        {
            if (IsRegistrationInProgress(chatId))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "✋ Сначала закончите заполнение анкеты — иначе я не смогу тебе помочь.\n" +
                    "Если хотите начать заново — напишите \"Cancel\".",
                    cancellationToken: cancellationToken);
                return false;
            }

            if (!await IsUserRegisteredAsync(chatId))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "⚠️ Похоже, вы ещё не заполнили анкету. Наберите /start, чтобы начать.",
                    cancellationToken: cancellationToken);
                return false;
            }

            return true;
        }

        private void ResetRegistrationState(long chatId)
        {
            _userSteps[chatId] = FillStatsStep.None;
            _userStats.Remove(chatId);
        }

        // ---------- /start и заполнение анкеты ----------

        public async Task HandleStartCommand(long chatId, Message message, CancellationToken cancellationToken)
        {
            if (IsRegistrationInProgress(chatId))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Вы уже в процессе заполнения анкеты. Продолжайте отвечать на вопросы, " +
                    "либо напишите \"Cancel\", чтобы начать заново.",
                    cancellationToken: cancellationToken);
                return;
            }

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

            // Если анкета уже когда-то была заполнена — не пересоздаём с нуля молча,
            // а предупреждаем, что все данные перезапишутся.
            if (await IsUserRegisteredAsync(chatId))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "У вас уже есть сохранённые данные. Если хотите их изменить — используйте \"Change own stats\".",
                    replyMarkup: Buttons.GetMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                "Hi there! I'm your personal nutrition bot. Let's start by filling up your stats.",
                cancellationToken: cancellationToken);

            _userSteps[chatId] = FillStatsStep.WaitingHeight;
            _userStats[chatId] = (null, null, null, null, null);

            await _botClient.SendTextMessageAsync(
                chatId,
                $"Enter your height in centimeters ({MinHeightCm}-{MaxHeightCm} cm):",
                cancellationToken: cancellationToken);
        }

        public async Task HandleChangeOwnStatsCommand(long chatId, CancellationToken cancellationToken)
        {
            if (IsRegistrationInProgress(chatId))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "You are already in the process of updating your stats. Please continue, " +
                    "or write \"Cancel\" to stop.",
                    cancellationToken: cancellationToken);
                return;
            }

            _userSteps[chatId] = FillStatsStep.WaitingHeight;
            _userStats[chatId] = (null, null, null, null, null);

            await _botClient.SendTextMessageAsync(
                chatId,
                $"Let's update your stats. Please enter your height in centimeters ({MinHeightCm}-{MaxHeightCm} cm):",
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Обрабатывает ввод во время анкеты. Возвращает true, если текст был "служебным"
        /// (относился к анкете) и его не нужно передавать дальше.
        /// </summary>
        public async Task<bool> HandlerFillStats(long chatId, string messageText, CancellationToken cancellationToken)
        {
            if (!_userSteps.TryGetValue(chatId, out var step) || step == FillStatsStep.None)
                return false;

            if (CancelWords.Contains(messageText.Trim().ToLowerInvariant()))
            {
                ResetRegistrationState(chatId);
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Registration cancelled. You can start again anytime with /start.",
                    replyMarkup: Buttons.GetMainMenu(),
                    cancellationToken: cancellationToken);
                return true;
            }

            switch (step)
            {
                case FillStatsStep.WaitingHeight:
                    await HandleHeightInput(chatId, messageText, cancellationToken);
                    return true;

                case FillStatsStep.WaitingWeight:
                    await HandleWeightInput(chatId, messageText, cancellationToken);
                    return true;

                case FillStatsStep.WaitingGender:
                    await HandleGenderInput(chatId, messageText, cancellationToken);
                    return true;

                case FillStatsStep.WaitingGoal:
                    await HandleGoalInput(chatId, messageText, cancellationToken);
                    return true;

                case FillStatsStep.WaitingActivity:
                    await HandleActivityInputAndSave(chatId, messageText, cancellationToken);
                    return true;
            }

            return false;
        }

        private async Task HandleHeightInput(long chatId, string messageText, CancellationToken cancellationToken)
        {
            if (!int.TryParse(messageText.Trim(), out int height))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "That doesn't look like a number 🙂 Please enter your height in centimeters, e.g. 175.",
                    cancellationToken: cancellationToken);
                return;
            }

            if (height < MinHeightCm || height > MaxHeightCm)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"Please enter a realistic height between {MinHeightCm} and {MaxHeightCm} cm.",
                    cancellationToken: cancellationToken);
                return;
            }

            var current = _userStats[chatId];
            _userStats[chatId] = (height, current.Weight, current.Gender, current.Goal, current.Activity);
            _userSteps[chatId] = FillStatsStep.WaitingWeight;

            await _botClient.SendTextMessageAsync(
                chatId,
                $"Enter your weight in kilograms ({MinWeightKg}-{MaxWeightKg} kg):",
                cancellationToken: cancellationToken);
        }

        private async Task HandleWeightInput(long chatId, string messageText, CancellationToken cancellationToken)
        {
            if (!int.TryParse(messageText.Trim(), out int weight))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "That doesn't look like a number 🙂 Please enter your weight in kilograms, e.g. 70.",
                    cancellationToken: cancellationToken);
                return;
            }

            if (weight < MinWeightKg || weight > MaxWeightKg)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"Please enter a realistic weight between {MinWeightKg} and {MaxWeightKg} kg.",
                    cancellationToken: cancellationToken);
                return;
            }

            var current = _userStats[chatId];
            _userStats[chatId] = (current.Height, weight, current.Gender, current.Goal, current.Activity);
            _userSteps[chatId] = FillStatsStep.WaitingGender;

            await _botClient.SendTextMessageAsync(
                chatId,
                "Select your gender:",
                replyMarkup: Buttons.GetGenderButtons(),
                cancellationToken: cancellationToken);
        }

        private async Task HandleGenderInput(long chatId, string messageText, CancellationToken cancellationToken)
        {
            var match = ValidGenders.FirstOrDefault(g => string.Equals(g, messageText.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Please use the buttons below to select your gender 🙂",
                    replyMarkup: Buttons.GetGenderButtons(),
                    cancellationToken: cancellationToken);
                return;
            }

            var current = _userStats[chatId];
            _userStats[chatId] = (current.Height, current.Weight, match, current.Goal, current.Activity);
            _userSteps[chatId] = FillStatsStep.WaitingGoal;

            await _botClient.SendTextMessageAsync(
                chatId,
                "Select your goal:",
                replyMarkup: Buttons.GetGoalButtons(),
                cancellationToken: cancellationToken);
        }

        private async Task HandleGoalInput(long chatId, string messageText, CancellationToken cancellationToken)
        {
            var match = ValidGoals.FirstOrDefault(g => string.Equals(g, messageText.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Please use the buttons below to select your goal 🙂",
                    replyMarkup: Buttons.GetGoalButtons(),
                    cancellationToken: cancellationToken);
                return;
            }

            var current = _userStats[chatId];
            _userStats[chatId] = (current.Height, current.Weight, current.Gender, match, current.Activity);
            _userSteps[chatId] = FillStatsStep.WaitingActivity;

            await _botClient.SendTextMessageAsync(
                chatId,
                "Select your activity level:",
                replyMarkup: Buttons.GetActivityButtons(),
                cancellationToken: cancellationToken);
        }

        private async Task HandleActivityInputAndSave(long chatId, string messageText, CancellationToken cancellationToken)
        {
            var match = ValidActivities.FirstOrDefault(a => string.Equals(a, messageText.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Please use the buttons below to select your activity level 🙂",
                    replyMarkup: Buttons.GetActivityButtons(),
                    cancellationToken: cancellationToken);
                return;
            }

            var current = _userStats[chatId];
            _userStats[chatId] = (current.Height, current.Weight, current.Gender, current.Goal, match);
            _userSteps[chatId] = FillStatsStep.Done;

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
                $"Thanks! Your stats are saved:\nHeight: {_userStats[chatId].Height} cm\nWeight: {_userStats[chatId].Weight} kg\n" +
                $"Gender: {_userStats[chatId].Gender}\nGoal: {_userStats[chatId].Goal}\nActivity Level: {_userStats[chatId].Activity}",
                replyMarkup: Buttons.GetMainMenu(),
                cancellationToken: cancellationToken);

            ResetRegistrationState(chatId);
        }

        // ---------- Остальные команды (требуют завершённой регистрации) ----------

        public async Task HandleSettingsCommand(long chatId, CancellationToken cancellationToken)
        {
            if (!await EnsureCanUseFeatureAsync(chatId, cancellationToken)) return;

            await _botClient.SendTextMessageAsync(
                chatId,
                "Settings menu",
                replyMarkup: Buttons.Settings(),
                cancellationToken: cancellationToken);
        }

        public async Task HandleBackCommand(long chatId, CancellationToken cancellationToken)
        {
            if (IsRegistrationInProgress(chatId))
            {
                ResetRegistrationState(chatId);
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Registration cancelled. You can start again anytime with /start.",
                    replyMarkup: Buttons.GetMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                "You are back to the main menu.",
                replyMarkup: Buttons.GetMainMenu(),
                cancellationToken: cancellationToken);
        }

        public async Task HandleEatCommand(long chatId, CancellationToken cancellationToken)
        {
            if (!await EnsureCanUseFeatureAsync(chatId, cancellationToken)) return;

            try
            {
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

            if (!_waitingForFoodPhoto.TryGetValue(chatId, out bool waiting) || !waiting)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "I wasn't expecting a photo right now. Use \"Eat\" first if you want to log food.",
                    cancellationToken: cancellationToken);
                return;
            }

            _waitingForFoodPhoto[chatId] = false;

            var photo = message.Photo[^1];
            var file = await _botClient.GetFileAsync(photo.FileId);
            var filePath = file.FilePath;
            var fileUrl = $"https://api.telegram.org/file/bot{_botToken}/{filePath}";

            byte[] photoBytes;
            using (var httpClient = new HttpClient())
            {
                photoBytes = await httpClient.GetByteArrayAsync(fileUrl);
            }

            var result = await _googleVisionService.RecognizeFoodAsync(photoBytes);
            await _botClient.SendTextMessageAsync(chatId, $"🍽 I see: {result}", cancellationToken: cancellationToken);

            var foodList = await _usdaService.GetFoodInfoAsync(result);
            var food = foodList.FirstOrDefault();

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

            if (food.IsHealthy)
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

        public async Task HandleHelpCommand(long chatId, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "Here are the commands you can use:\n" +
                "/start - Start the bot and fill your stats\n" +
                "Change own stats - Change your stats\n" +
                "Daily Goal - Show your daily goals\n" +
                "Eat - Log your food intake\n" +
                "Shop - View the shop\n" +
                "Stats - View your stats\n" +
                "Settings - Change settings\n" +
                "Back - Go back to the main menu / cancel current step",
                cancellationToken: cancellationToken);
        }

        public async Task HandleDailyGoalCommand(long chatId, CancellationToken cancellationToken)
        {
            if (!await EnsureCanUseFeatureAsync(chatId, cancellationToken)) return;

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
            if (!await EnsureCanUseFeatureAsync(chatId, cancellationToken)) return;

            var shopService = new ShopService(_ProductsRepository);
            await shopService.ShowShopAsync(_botClient, chatId);
        }

        public async Task HandleStatsCommand(long chatId, CancellationToken cancellationToken)
        {
            if (!await EnsureCanUseFeatureAsync(chatId, cancellationToken)) return;

            var stats = await _statsOfUsersRepository.GetStatsByTelegramIdAsync(chatId);
            if (stats == null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "Please fill your stats first using /start command.",
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
            // Если пользователь сейчас заполняет анкету — это приоритетный обработчик.
            if (await HandlerFillStats(chatId, messageText, cancellationToken))
                return;

            // Иначе это просто произвольный текст, не совпавший ни с одной командой.
            if (!await IsUserRegisteredAsync(chatId))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "I didn't understand that. Please use /start to register first.",
                    cancellationToken: cancellationToken);
                return;
            }

            await _botClient.SendTextMessageAsync(
                chatId,
                "I haven't understood. Type \"Help\" to see the list of available commands.",
                cancellationToken: cancellationToken);
        }

        public async Task HandleUnknownCommand(long chatId, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
                chatId,
                "I don't recognize that command. Type \"Help\" to see what I can do.",
                cancellationToken: cancellationToken);
        }
    }
}