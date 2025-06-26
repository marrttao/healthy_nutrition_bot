using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace HealthyNutritionBot.domain.entities;

[Table("stats_of_users")]
public class StatsOfUsers : BaseModel
{
    [PrimaryKey("id", false)]
    public int id { get; set; }

    [Reference(typeof(User))]
    [Column("telegram_id")]
    public long TelegramId { get; set; }

    [Column("weight")]
    public float Weight { get; set; }

    [Column("height")]
    public float Height { get; set; }
    
    [Column("points")]
    public int Points { get; set; }
    
    [Column("shoppoints")]
    public int ShopPoints { get; set; }
    
    [Column("gender")]
    public string Gender { get; set; }
    
    [Column("goal")]
    public string Goal { get; set; }
    
    [Column("activity")]
    public string Activity { get; set; }
}