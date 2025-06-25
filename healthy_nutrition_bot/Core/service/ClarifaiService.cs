using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using TokenReaderClass = HealthyNutritionBot.service.TokenReader.TokenReader;

namespace HealthyNutritionBot.service;

public class ClarifaiService
{
    private readonly string _clarifaiToken;
    private readonly HttpClient _httpClient;

    public ClarifaiService(TokenReaderClass tokenReader)
    {
        _clarifaiToken = tokenReader.GetClarifaiToken();
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Key", _clarifaiToken);
    }

    public async Task<string> AnalyzeImageAsync(string imageUrl)
    {
        var requestBody = new
        {
            inputs = new[]
            {
                new
                {
                    data = new
                    {
                        image = new { url = imageUrl }
                    }
                }
            }
        };
        var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://api.clarifai.com/v2/models/general-image-recognition/outputs", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> RecognizeFoodAsync(string imageUrl)
    {
        var requestBody = new
        {
            inputs = new[]
            {
                new
                {
                    data = new
                    {
                        image = new { url = imageUrl }
                    }
                }
            }
        };
        var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://api.clarifai.com/v2/users/clarifai/apps/main/models/food-item-recognition/versions/1d5fd481e0cf4826aa72ec3ff049e044/outputs", content);
        response.EnsureSuccessStatusCode();
        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var concept = doc.RootElement
            .GetProperty("outputs")[0]
            .GetProperty("data")
            .GetProperty("concepts")[0]
            .GetProperty("name")
            .GetString();
        return concept ?? "Unknown food";
    }
}