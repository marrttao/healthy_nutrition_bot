using HealthyNutritionBot.domain.interfaces;
using HealthyNutritionBot.domain.entities;
using System;
using System.Threading.Tasks;

namespace HealthyNutritionBot.domain.utils;

public class NutritionCalculator
{
    private readonly IDailyNormRepository _dailyNormRepository;

    public string Gender { get; private set; }
    public double WeightKg { get; private set; }
    public double HeightCm { get; private set; }
    public string ActivityLevel { get; private set; }
    public string Goal { get; private set; }

    public NutritionCalculator(
        double heightCm,
        double weightKg,
        string gender,
        string goal,
        string activityLevel,
        IDailyNormRepository dailyNormRepository)
    {
        Gender = gender.Trim().ToLower();
        WeightKg = weightKg;
        HeightCm = heightCm;
        ActivityLevel = activityLevel.Trim().ToLower();
        Goal = goal.Trim().ToLower();
        _dailyNormRepository = dailyNormRepository;
    }

    private double CalculateBmr()
    {
        const int defaultAge = 16;
        return Gender == "male"
            ? 10 * WeightKg + 6.25 * HeightCm - 5 * defaultAge + 5
            : 10 * WeightKg + 6.25 * HeightCm - 5 * defaultAge - 161;
    }

    private double GetActivityMultiplier()
    {
        return ActivityLevel switch
        {
            "low" => 1.2,
            "medium" => 1.55,
            "high" => 1.75,
            _ => 1.2
        };
    }

    private double AdjustCaloriesForGoal(double calories)
    {
        return Goal switch
        {
            "lose" => calories - 300,
            "gain" => calories + 300,
            _ => calories
        };
    }

    public (double Calories, double Protein, double Fat, double Carbs) CalculateNutritionGoals()
    {
        double bmr = CalculateBmr();
        double tdee = bmr * GetActivityMultiplier();
        double targetCalories = AdjustCaloriesForGoal(tdee);

        double protein = WeightKg * 2;
        double fat = WeightKg * 1;
        double proteinCalories = protein * 4;
        double fatCalories = fat * 9;
        double carbsCalories = targetCalories - proteinCalories - fatCalories;
        double carbs = carbsCalories / 4;

        return (
            Math.Round(targetCalories),
            Math.Round(protein),
            Math.Round(fat),
            Math.Round(carbs)
        );
    }

    public async Task<string> GetNutritionReportAsync(long telegramId)
    {
        var goals = CalculateNutritionGoals();
        var dailyNorm = await _dailyNormRepository.GetDailyNorm(telegramId);

        if (dailyNorm == null)
        {
            dailyNorm = new DailyNorm
            {
                TelegramId = telegramId,
                Calories = goals.Calories,
                Proteins = goals.Protein,
                Fats = goals.Fat,
                Carbs = goals.Carbs
            };
            await _dailyNormRepository.AddDailyNorm(dailyNorm);
        }
        else
        {
            dailyNorm.Calories = goals.Calories;
            dailyNorm.Proteins = goals.Protein;
            dailyNorm.Fats = goals.Fat;
            dailyNorm.Carbs = goals.Carbs;
            await _dailyNormRepository.UpdateDailyNorm(dailyNorm);
        }

        return $"Your daily nutrition goals:\n" +
               $"🔥 Calories: {goals.Calories} kcal\n" +
               $"🥩 Protein: {goals.Protein} g\n" +
               $"🧈 Fat: {goals.Fat} g\n" +
               $"🍞 Carbs: {goals.Carbs} g";
    }
}