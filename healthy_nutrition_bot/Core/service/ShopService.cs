using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;
using Telegram.Bot.Types.ReplyMarkups;
using System.Threading;
using System.Threading.Tasks;
using HealthyNutritionBot.domain.interfaces;
using HealthyNutritionBot.domain.entities;
using System.Linq;

namespace healthy_nutrition_bot.Core.service;

public class ShopService
{
    private readonly IProductsRepository _productsRepository;

    public ShopService(IProductsRepository productsRepository)
    {
        _productsRepository = productsRepository ?? throw new ArgumentNullException(nameof(productsRepository));
    }

    public async Task ShowShopAsync(ITelegramBotClient botClient, long chatId)
    {
        var products = await _productsRepository.GetAllProductsAsync();
        Console.WriteLine($"Products count: {products?.Count()}"); // For debugging
        if (products == null || !products.Any())
        {
            await botClient.SendTextMessageAsync(chatId, "No products available.");
            return;
        }

        foreach (var product in products)
        {
            var prices = new[] { new LabeledPrice(product.Name, (int)(product.Price * 100)) };
            await botClient.SendInvoiceAsync(
                chatId: chatId,
                title: product.Name,
                description: $"Цена: {product.Price} {product.Currency}",
                payload: $"product-{product.id}",
                providerToken: "1877036958:TEST:374256dd2014a6dd628c2a487d7bae8cde5dc0bb", // Use a real token
                currency: product.Currency,
                prices: prices,
                photoUrl: product.ImgUrl,
                photoWidth: 512,
                photoHeight: 512,
                isFlexible: false,
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithPayment("Оплатить")
                )
            );
        }
    }

    private static async Task HandlePreCheckoutQueryAsync(ITelegramBotClient botClient, PreCheckoutQuery preCheckoutQuery)
    {
        await botClient.AnswerPreCheckoutQueryAsync(
            preCheckoutQuery.Id,
            cancellationToken: CancellationToken.None
        );
    }

    private static async Task HandleSuccessfulPaymentAsync(ITelegramBotClient botClient, Message message)
    {
        var payment = message.SuccessfulPayment;

        await botClient.SendTextMessageAsync(
            message.Chat.Id,
            $"Платеж на сумму {payment.TotalAmount / 100} {payment.Currency.ToString()} успешно получен!\n" +
            $"Товар: {payment.InvoicePayload.ToString()}\n" +
            "Спасибо за покупку!"
        );
    }
}
