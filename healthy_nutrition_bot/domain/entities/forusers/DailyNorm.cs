using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace HealthyNutritionBot.domain.entities;

[Table("daily_norm")]
public class DailyNorm : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }

    [Reference(typeof(User))]
    [Column("telegram_id")]
    public long TelegramId { get; set; }

    [Column("calories")]
    public double Calories { get; set; }

    [Column("protein")]
    public double Proteins { get; set; }

    [Column("fats")]
    public double Fats { get; set; }

    [Column("carbs")]
    public double Carbs { get; set; }
    
    [Column("calories_today")]
    public double CaloriesToday { get; set; }
    
    [Column("protein_today")]
    public double ProteinsToday { get; set; }
    
    [Column("fat_today")]
    public double FatsToday { get; set; }
    
    [Column("carbs_today")]
    public double CarbsToday { get; set; }
}