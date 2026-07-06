namespace HealthyNutritionBot.service.TokenReader;

public class TokenReader
{
    private string GetEnvironmentVariable(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException($"Environment variable '{variableName}' is not set or is empty. Please check your .env file.");
        return value;
    }

    public TokenReader()
    {
        // Constructor no longer requires a file path
    }

    public string GetTelegramToken() => GetEnvironmentVariable("TELEGRAM_TOKEN");
    public string GetSupabaseUrl() => GetEnvironmentVariable("SUPABASE_URL");
    public string GetSupabaseKey() => GetEnvironmentVariable("SUPABASE_KEY");
    public string GetGoogleVision() => GetEnvironmentVariable("GOOGLE_VISION_API");
    public string GetUsdaApiKey() => GetEnvironmentVariable("USDA_API_KEY");
    public string GetProviderToken() => GetEnvironmentVariable("PROVIDER_TOKEN");
    public string GetConnectionString() => GetEnvironmentVariable("SUPABASE_CONNECTION_STRING");
}

