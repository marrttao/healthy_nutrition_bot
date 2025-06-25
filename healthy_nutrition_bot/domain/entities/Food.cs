namespace healthy_nutrition_bot.domain.entities;

public class Food
{
    public string Description { get; set; } = string.Empty;
    public List<Nutrient> FoodNutrients { get; set; } = new List<Nutrient>();
}