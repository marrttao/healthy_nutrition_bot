using Telegram.Bot.Types.ReplyMarkups;

namespace healthy_nutrition_bot.UI;

public static class Buttons
{
    public static ReplyKeyboardMarkup GetMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Eat", "Daily Goal"  },
            new KeyboardButton[] { "Stats", "Ranking", "Shop" },
            new KeyboardButton[] { "Settings", "Help" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
    }

    public static ReplyKeyboardMarkup Back()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Back" }
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
            new KeyboardButton[] { "Change own stats"},
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
            new[] { new KeyboardButton("Male"), new KeyboardButton("Female") }
        })
        {
            ResizeKeyboard = true
        };
    }

    public static ReplyKeyboardMarkup GetGoalButtons()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Lose"), new KeyboardButton("Maintain") },
            new[] { new KeyboardButton("Gain")}
        })
        {
            ResizeKeyboard = true
        };
    }

    public static ReplyKeyboardMarkup GetActivityButtons()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Low"), new KeyboardButton("Medium") },
            new[] { new KeyboardButton("High")}
        })
        {
            ResizeKeyboard = true
        };
    }
    
    
}