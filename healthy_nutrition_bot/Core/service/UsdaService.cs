using System.Text.Json;
using Microsoft.Extensions.Configuration;
using UsdaFoodApi.Models;
using healthy_nutrition_bot.domain.entities;
using Microsoft.IdentityModel.Tokens;
using healthy_nutrition_bot.Core.service;

namespace healthy_nutrition_bot.Core.service;

    public class UsdaService
    {
        private readonly HttpClient _httpClient;
        private string _apiKey {get; set; }

        public UsdaService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be empty.", nameof(apiKey));

            _apiKey = apiKey;
        
        
            
            _httpClient = new HttpClient();
        }

        public async Task<List<FoodNutrientResponse>> GetFoodInfoAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty.");

            var url = $"https://api.nal.usda.gov/fdc/v1/foods/search?query={Uri.EscapeDataString(query)}&api_key={_apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception($"USDA API request failed: {response.StatusCode}, Response: {content}");
            }

            var json = await response.Content.ReadAsStringAsync();

            var searchResult = JsonSerializer.Deserialize<FoodSearchResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new Exception("Failed to deserialize USDA API response.");

            var results = new List<FoodNutrientResponse>();

            foreach (var food in searchResult.Foods)
            {
                var nutrientResponse = new FoodNutrientResponse { FoodName = food.Description };
                double fiber = 0;

                foreach (var nutrient in food.FoodNutrients)
                {
                    switch (nutrient.NutrientName.ToLower())
                    {
                        case "energy":
                            if (nutrient.UnitName.ToLower() == "kcal")
                                nutrientResponse.Calories = nutrient.Value;
                            break;
                        case "protein":
                            nutrientResponse.Protein = nutrient.Value;
                            break;
                        case "total lipid (fat)":
                            nutrientResponse.Fat = nutrient.Value;
                            break;
                        case "carbohydrate, by difference":
                            nutrientResponse.Carbohydrates = nutrient.Value;
                            break;
                        case "fiber, total dietary":
                            fiber = nutrient.Value;
                            break;
                    }
                }

                nutrientResponse.IsHealthy = IsHealthyFood(
                    nutrientResponse.Calories,
                    nutrientResponse.Protein,
                    nutrientResponse.Fat,
                    nutrientResponse.Carbohydrates,
                    fiber);

                results.Add(nutrientResponse);
            }

            return results;
        }

        private bool IsHealthyFood(double calories, double protein, double fat, double carbohydrates, double fiber)
        {
            bool isLowCalorie = calories < 200;
            bool isLowFat = fat < 10;
            bool isHighProtein = protein > 5;
            bool isModerateCarbs = carbohydrates < 30;
            bool isHighFiber = fiber > 2;

            int criteriaMet = 0;
            if (isLowCalorie) criteriaMet++;
            if (isLowFat) criteriaMet++;
            if (isHighProtein) criteriaMet++;
            if (isModerateCarbs) criteriaMet++;
            if (isHighFiber) criteriaMet++;

            return criteriaMet >= 3;
        }
    }

