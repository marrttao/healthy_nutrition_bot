using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthyNutritionBot.domain.entities;

[Table("stats_of_users")]
public class StatsOfUsers // Убрали наследование BaseModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; } // Исправлено имя переменной с id на Id

    [Column("telegram_id")]
    public long TelegramId { get; set; }

    [Column("weight")]
    public int Weight { get; set; }

    [Column("height")]
    public int Height { get; set; }
    
    [Column("points")]
    public int Points { get; set; }
    
    [Column("shoppoints")]
    public int ShopPoints { get; set; }
    
    [Column("gender")]
    public string Gender { get; set; } = string.Empty;
    
    [Column("goal")]
    public string Goal { get; set; } = string.Empty;
    
    [Column("activity")]
    public string Activity { get; set; } = string.Empty;
}