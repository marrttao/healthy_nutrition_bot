namespace UsdaFoodApi.Models;

public class FoodNutrientResponse
{
    public string FoodName { get; set; } = string.Empty;
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Carbohydrates { get; set; }
    public bool IsHealthy { get; set; }
}