using HealthyNutritionBot.domain.entities;
using System.Threading.Tasks;
using System.Linq;
using HealthyNutritionBot.service;
using HealthyNutritionBot.domain.interfaces;

namespace HealthyNutritionBot.domain.repositories;

public class DailyNormRepository : IDailyNormRepository
{
    private readonly Supabase.Client _supabase;
    private readonly FetchService _fetchService;
    private readonly InsertService _insertService;
    private readonly UpdateService _updateService;

    public DailyNormRepository(Supabase.Client supabase)
    {
        _supabase = supabase;
        _fetchService = new FetchService(supabase);
        _insertService = new InsertService(supabase);
        _updateService = new UpdateService(supabase);
    }

    public async Task<DailyNorm> GetDailyNorm(long telegramId)
    {
        var dailyNorm = await _fetchService.GetDataByConditionAsync<DailyNorm>("daily_norm", x => x.TelegramId == telegramId);
        return dailyNorm.FirstOrDefault();
    }

    public async Task AddDailyNorm(DailyNorm dailyNorm)
    {
        await _insertService.InsertAsync(dailyNorm);
    }

    public async Task UpdateDailyNorm(DailyNorm dailyNorm)
    {
        await _updateService.UpdateAsync(dailyNorm);
    }
}