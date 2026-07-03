using HealthyNutritionBot.domain.entities;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HealthyNutritionBot.domain.interfaces;
using HealthyNutritionBot.Data; // Подключаем AppDbContext

namespace HealthyNutritionBot.domain.repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserById(long id)
    {
        // EF Core сам транслирует это в SQL запрос к базе
        return await _context.Users.FirstOrDefaultAsync(x => x.TelegramId == id);
    }

    public async Task AddUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
        // Обязательно сохраняем изменения
        await _context.SaveChangesAsync(); 
    }
}