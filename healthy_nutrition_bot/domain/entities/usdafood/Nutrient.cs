namespace healthy_nutrition_bot.domain.entities;

public class Nutrient
{
    public string NutrientName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public double Value { get; set; }
}