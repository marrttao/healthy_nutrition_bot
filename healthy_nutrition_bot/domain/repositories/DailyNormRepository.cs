using HealthyNutritionBot.domain.entities;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HealthyNutritionBot.domain.interfaces;
using HealthyNutritionBot.Data; // Подключаем AppDbContext

namespace HealthyNutritionBot.domain.repositories;

public class DailyNormRepository : IDailyNormRepository
{
    private readonly AppDbContext _context;

    public DailyNormRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DailyNorm?> GetDailyNorm(long telegramId)
    {
        return await _context.DailyNorms.FirstOrDefaultAsync(x => x.TelegramId == telegramId);
    }

    public async Task AddDailyNorm(DailyNorm dailyNorm)
    {
        await _context.DailyNorms.AddAsync(dailyNorm);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateDailyNorm(DailyNorm dailyNorm)
    {
        _context.DailyNorms.Update(dailyNorm);
        await _context.SaveChangesAsync();
    }
}