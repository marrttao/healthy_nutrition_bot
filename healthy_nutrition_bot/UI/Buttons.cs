using Telegram.Bot.Types.ReplyMarkups;

namespace healthy_nutrition_bot.UI;

public static class Buttons
{
    public static ReplyKeyboardMarkup GetMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Eat", "Daily Goal"  },
            new KeyboardButton[] { "Stats", "Ranking" },
            new KeyboardButton[] { "Settings", "Help" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
    }

    public static ReplyKeyboardMarkup Settings()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Change own stats", "Change Language" },
            new KeyboardButton[] { "Back" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
    }
    
    public static ReplyKeyboardMarkup GetGenderButtons()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Male"), new KeyboardButton("Female") },
            new[] { new KeyboardButton("Other") }
        })
        {
            ResizeKeyboard = true
        };
    }

    public static ReplyKeyboardMarkup GetGoalButtons()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Lose Weight"), new KeyboardButton("Maintain Weight") },
            new[] { new KeyboardButton("Gain Weight"), new KeyboardButton("Build Muscle") }
        })
        {
            ResizeKeyboard = true
        };
    }

    public static ReplyKeyboardMarkup GetActivityButtons()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Sedentary"), new KeyboardButton("Light Activity") },
            new[] { new KeyboardButton("Moderate Activity"), new KeyboardButton("Very Active") },
            new[] { new KeyboardButton("Extra Active") }
        })
        {
            ResizeKeyboard = true
        };
    }
    
    
}