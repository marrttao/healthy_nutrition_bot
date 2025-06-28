using System.Text.Json;

namespace HealthyNutritionBot.service.TokenReader;

public class  TokenReader
{
    private class BotSettings
    {
        public string TelegramToken { get; set; } = string.Empty;
        public string SupabaseUrl { get; set; } = string.Empty;
        public string SupabaseKey { get; set; } = string.Empty;
        public string ClarifaiToken { get; set; } = string.Empty;
        
        public string UsdaApiKey { get; set; } = string.Empty;
        
        public string providerToken { get; set; } = string.Empty;
    }

    private class TokensFile
    {
        public BotSettings BotSettings { get; set; } = new();
    }

    private readonly BotSettings _settings;

    public TokenReader(string filePath = "/home/marrttao/RiderProjects/healthy_nutrition_bottttt/healthy_nutrition_bot/Tokens.json")
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var tokens = JsonSerializer.Deserialize<TokensFile>(json)
                     ?? throw new Exception("Failed to parse tokens file.");
        _settings = tokens.BotSettings;
    }

    public string GetTelegramToken() => _settings.TelegramToken;
    public string GetSupabaseUrl() => _settings.SupabaseUrl;
    public string GetSupabaseKey() => _settings.SupabaseKey;
    public string GetClarifaiToken() => _settings.ClarifaiToken;
    
    public string GetUsdaApiKey() => _settings.UsdaApiKey;
    public string GetProviderToken() => _settings.providerToken;
}

