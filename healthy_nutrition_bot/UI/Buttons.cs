using Telegram.Bot.Types.ReplyMarkups;

namespace healthy_nutrition_bot.UI;

public static class Buttons
{
    public static ReplyKeyboardMarkup GetMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Add Food", "Remove Food" },
            new KeyboardButton[] { "Stats", "Points" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
    }
}