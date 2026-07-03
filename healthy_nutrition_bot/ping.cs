namespace HealthyNutritionBot;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
public static class ping
{
    public static IEndpointRouteBuilder MapPingEndpoint(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/ping", () =>
        {
            return Results.Ok(new 
            { 
                status = "ok", 
                message = "Telegram Bot is awake!", 
                timestamp = DateTime.UtcNow 
            });
        });

        return builder;
    }
}