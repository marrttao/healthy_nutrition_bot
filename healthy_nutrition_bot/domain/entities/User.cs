using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace HealthyNutritionBot.domain.entities;

[Table("users")]
public class User : BaseModel
{
    [PrimaryKey("telegram_id", false)]
    public short TelegramId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } // заменить TimestampAttribute на DateTime

    [Column("name")]
    public string name { get; set; } = string.Empty;

    [Column("lastname")]
    public string lastname { get; set; } = string.Empty;

    [Column("isActive")]
    public bool isActive { get; set; } = true;
}   