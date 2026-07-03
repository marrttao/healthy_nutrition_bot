using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using TokenReaderClass = HealthyNutritionBot.service.TokenReader.TokenReader;

namespace HealthyNutritionBot.service;

public class GoogleVisionService
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public GoogleVisionService(TokenReaderClass tokenReader)
    {
        _apiKey = tokenReader.GetGoogleVision(); 
        _httpClient = new HttpClient();
    }

    public async Task<string> RecognizeFoodAsync(byte[] imageBytes)
    {
        var url = $"https://vision.googleapis.com/v1/images:annotate?key={_apiKey}";
        var base64Image = Convert.ToBase64String(imageBytes);

        var requestBody = new
        {
            requests = new[]
            {
                new
                {
                    image = new { content = base64Image },
                    features = new[] { new { type = "LABEL_DETECTION", maxResults = 10 } }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);
        
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"DEBUG: Полный JSON от Google: {jsonResponse}");

        if (!response.IsSuccessStatusCode) return "Service Error";
    
        var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement;

        if (root.TryGetProperty("responses", out var responses) && responses.GetArrayLength() > 0)
        {
            var firstResponse = responses[0];
        
            if (firstResponse.TryGetProperty("error", out var error))
            {
                Console.WriteLine($"DEBUG: Ошибка Vision API: {error.GetRawText()}");
                return "Vision API Error";
            }

            if (firstResponse.TryGetProperty("labelAnnotations", out var labels) && labels.GetArrayLength() > 0)
            {
                // Список слов, которые мы хотим пропустить, чтобы добраться до названия блюда
                var skipList = new List<string> { "Food", "Ingredient", "Cuisine", "Dish", "Meal", "Cooking", "Tableware", "Bowl", "Plate" };
                
                foreach (var label in labels.EnumerateArray())
                {
                    string description = label.GetProperty("description").GetString();
                    
                    // Если описание не входит в список мусорных слов - используем его
                    if (!skipList.Contains(description, StringComparer.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"DEBUG: Google увидел конкретный объект: {description}");
                        return description;
                    }
                }

                // Если ничего конкретнее не нашлось, вернем хотя бы первое (как запасной вариант)
                string fallback = labels[0].GetProperty("description").GetString();
                return fallback;
            }
        }

        Console.WriteLine("DEBUG: Ответ получен, но меток нет.");
        return "No food detected";
    }
}