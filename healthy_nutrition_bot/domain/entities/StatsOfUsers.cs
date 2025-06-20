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

    [Column("sex")]
    public string Sex { get; set; } = string.Empty;

    [Column("points")]
    public int Points { get; set; }
}