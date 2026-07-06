using HealthyNutritionBot.domain.entities;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;
using HealthyNutritionBot.domain.interfaces;
using HealthyNutritionBot.Data; // Подключаем AppDbContext

namespace HealthyNutritionBot.domain.repositories;

public class StatsOfUsersRepository : IStatsOfUsersRepository
{
    private readonly AppDbContext _context;
    
    public StatsOfUsersRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StatsOfUsers?> GetStatsByTelegramIdAsync(long telegramId)
    {
        return await _context.StatsOfUsers.FirstOrDefaultAsync(x => x.TelegramId == telegramId);
    }

    public async Task AddStatsAsync(StatsOfUsers stats)
    {
        await _context.StatsOfUsers.AddAsync(stats);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatsAsync(StatsOfUsers stats)
    {
        if (stats == null)
            throw new ArgumentNullException(nameof(stats), "Stats object cannot be null.");
            
        _context.StatsOfUsers.Update(stats);
        await _context.SaveChangesAsync();
    }
}