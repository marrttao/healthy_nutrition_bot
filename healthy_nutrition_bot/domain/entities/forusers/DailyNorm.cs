using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthyNutritionBot.domain.entities;

[Table("daily_norm")]
public class DailyNorm 
{ // Добавлена пропущенная открывающая скобка

    [Key] // Заменили PrimaryKey на Key
    [Column("id")]
    public int Id { get; set; }

    // Атрибут [Reference] удален, EF Core свяжет таблицы по ключу, если это потребуется
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