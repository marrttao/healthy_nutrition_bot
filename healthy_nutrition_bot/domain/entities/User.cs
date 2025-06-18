// User.cs
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace HealthyNutritionBot.domain.entities;

[Table("users")]
public class User : BaseModel
{
    
    [Column("telegram_id")]
    public long TelegramId { get; set; }
    [PrimaryKey("id", false)]
    public short id { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("lastname")]
    public string Lastname { get; set; } = string.Empty;

    [Column("isActive")]
    public bool IsActive { get; set; } = true;
}