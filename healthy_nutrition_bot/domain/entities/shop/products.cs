
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace HealthyNutritionBot.domain.entities;

[Table("products")]
public class Products : BaseModel
{
    [PrimaryKey("id", false)]
    public short id { get; set; }
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("price")]
    public double Price { get; set; }
    
    [Column("currency")]
    public string Currency { get; set; } = string.Empty;
    
    [Column("img_url")]
    public string ImgUrl { get; set; } = string.Empty;
}