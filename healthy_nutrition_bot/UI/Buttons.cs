using Telegram.Bot.Types.ReplyMarkups;

namespace healthy_nutrition_bot.UI;

public static class Buttons
{
    public static ReplyKeyboardMarkup GetMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Eat",  },
            new KeyboardButton[] { "Stats", "ranking" },
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
    
}