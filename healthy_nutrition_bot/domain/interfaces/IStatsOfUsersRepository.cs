using HealthyNutritionBot.domain.entities;
using System.Threading.Tasks;

namespace HealthyNutritionBot.domain.interfaces;

public interface IStatsOfUsersRepository
{
    Task<StatsOfUsers> GetStatsByTelegramIdAsync(long telegramId);
    Task AddStatsAsync(StatsOfUsers stats);
    
}