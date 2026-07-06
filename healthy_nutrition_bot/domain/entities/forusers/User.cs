using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthyNutritionBot.domain.entities;

[Table("users")]
public class User // Убрали наследование BaseModel
{
    [Key]
    [Column("id")]
    public short Id { get; set; } // Исправлено имя переменной на Id для единообразия

    [Column("telegram_id")]
    public long TelegramId { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("lastname")]
    public string Lastname { get; set; } = string.Empty;

    [Column("isActive")]
    public bool IsActive { get; set; } = true;
}