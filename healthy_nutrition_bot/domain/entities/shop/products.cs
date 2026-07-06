using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthyNutritionBot.domain.entities;

[Table("products")]
public class Products // Убрано наследование от BaseModel
{
    [Key] // Заменили атрибут Supabase на стандартный EF Core
    [Column("id")]
    public short Id { get; set; } // Изменили название с id на Id по стандартам C#
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("price")]
    public double Price { get; set; }
    
    [Column("currency")]
    public string Currency { get; set; } = string.Empty;
    
    [Column("img_url")]
    public string ImgUrl { get; set; } = string.Empty;
}