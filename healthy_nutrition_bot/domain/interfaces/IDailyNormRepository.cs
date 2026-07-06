using HealthyNutritionBot.domain.entities;
using System.Threading.Tasks;

namespace HealthyNutritionBot.domain.interfaces;

public interface IDailyNormRepository
{
    Task<DailyNorm> GetDailyNorm(long telegramId);
    Task AddDailyNorm(DailyNorm dailyNorm);
    Task UpdateDailyNorm(DailyNorm dailyNorm);
    
}