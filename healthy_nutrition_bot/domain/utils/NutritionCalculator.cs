using HealthyNutritionBot.domain.interfaces;
using HealthyNutritionBot.domain.entities;
using System;
using System.Threading.Tasks;

namespace HealthyNutritionBot.domain.utils;

public class NutritionCalculator
{
    private readonly IDailyNormRepository _dailyNormRepository;

    public string Gender { get; set; } // "male" or "female"
    public double WeightKg { get; set; }
    public double HeightCm { get; set; }
    public string ActivityLevel { get; set; } // "low", "medium", "high"
    public string Goal { get; set; } // "lose", "maintain", "gain"

    public NutritionCalculator(double heightCm, double weightKg, string gender, string goal, string activityLevel, IDailyNormRepository dailyNormRepository)
    {
        Gender = gender.ToLower();
        WeightKg = weightKg;
        HeightCm = heightCm;
        ActivityLevel = activityLevel.ToLower();
        Goal = goal.ToLower();
        _dailyNormRepository = dailyNormRepository;
    }

    public double CalculateBMR()
    {
        // Assuming default age = 16 for approximation
        int assumedAge = 16;
        return Gender == "male"
            ? 10 * WeightKg + 6.25 * HeightCm - 5 * assumedAge + 5
            : 10 * WeightKg + 6.25 * HeightCm - 5 * assumedAge - 161;
    }

    public double GetActivityFactor()
    {
        return ActivityLevel switch
        {
            "low" => 1.2,
            "medium" => 1.55,
            "high" => 1.75,
            _ => 1.2
        };
    }

    public double AdjustCalories(double baseCalories)
    {
        return Goal switch
        {
            "lose" => baseCalories - 300,
            "gain" => baseCalories + 300,
            _ => baseCalories // maintain
        };
    }

    public (double Calories, double Protein, double Fat, double Carbs) CalculateNutrition()
    {
        double bmr = CalculateBMR();
        double activityFactor = GetActivityFactor();
        double tdee = bmr * activityFactor;
        double adjustedCalories = AdjustCalories(tdee);

        double protein = WeightKg * 2; // grams
        double fat = WeightKg * 1;     // grams

        double proteinCalories = protein * 4;
        double fatCalories = fat * 9;
        double carbsCalories = adjustedCalories - proteinCalories - fatCalories;
        double carbs = carbsCalories / 4;

        return (Math.Round(adjustedCalories), Math.Round(protein), Math.Round(fat), Math.Round(carbs));
    }

    public async Task<string> GetNutritionReport(long telegramId)
    {
        // if not exists save to db
        var dailyNorm = await _dailyNormRepository.GetDailyNorm(telegramId);
        var nutritionValues = CalculateNutrition();

        if (dailyNorm == null)
        {
            dailyNorm = new DailyNorm
            {
                TelegramId = telegramId,
                Calories = (float)nutritionValues.Calories,
                Proteins = (float)nutritionValues.Protein,
                Fats = (float)nutritionValues.Fat,
                Carbs = (float)nutritionValues.Carbs
            };
            await _dailyNormRepository.AddDailyNorm(dailyNorm);
        }
        else
        {
            dailyNorm.Calories = (float)nutritionValues.Calories;
            dailyNorm.Proteins = (float)nutritionValues.Protein;
            dailyNorm.Fats = (float)nutritionValues.Fat;
            dailyNorm.Carbs = (float)nutritionValues.Carbs;
            await _dailyNormRepository.UpdateDailyNorm(dailyNorm);
        }

        return $"Your daily nutrition goals:\n" +
               $"🔥 Calories: {nutritionValues.Calories} kcal\n" +
               $"🥩 Protein: {nutritionValues.Protein} g\n" +
               $"🧈 Fat: {nutritionValues.Fat} g\n" +
               $"🍞 Carbs: {nutritionValues.Carbs} g";
    }
}