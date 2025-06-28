using HealthyNutritionBot.domain.entities;
using System.Threading.Tasks;
using System.Linq;
using HealthyNutritionBot.service;
using HealthyNutritionBot.domain.interfaces;

namespace HealthyNutritionBot.domain.repositories;

public class StatsOfUsersRepository : IStatsOfUsersRepository
{
    private readonly Supabase.Client _supabase;
    private readonly FetchService _fetchService;
    private readonly InsertService _insertService;
    private readonly UpdateService _updateService;
    
    public StatsOfUsersRepository(Supabase.Client supabase)
    {
        _supabase = supabase;
        _fetchService = new FetchService(supabase);
        _insertService = new InsertService(supabase);
        _updateService = new UpdateService(supabase);
    }

    public async Task<StatsOfUsers> GetStatsByTelegramIdAsync(long telegramId)
    {
        var stats = await _fetchService.GetDataByConditionAsync<StatsOfUsers>("stats_of_users", x => x.TelegramId == telegramId);
        return stats.FirstOrDefault();
    }

    public async Task AddStatsAsync(StatsOfUsers stats)
    {
        await _insertService.InsertAsync(stats);
    }

    public async Task UpdateStatsAsync(StatsOfUsers stats)
    {
        if (stats == null)
            throw new ArgumentNullException(nameof(stats), "Stats object cannot be null.");
        await _updateService.UpdateAsync(stats);
    }
}

