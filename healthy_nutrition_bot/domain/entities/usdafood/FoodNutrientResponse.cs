namespace UsdaFoodApi.Models;

public class FoodNutrientResponse
{
    public string FoodName { get; set; } = string.Empty;
    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Fat { get; set; }
    public float Carbohydrates { get; set; }
    public bool IsHealthy { get; set; }
}