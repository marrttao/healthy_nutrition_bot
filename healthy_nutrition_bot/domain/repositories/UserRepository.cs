// UserRepository.cs
using HealthyNutritionBot.domain.entities;
using System.Threading.Tasks;
using System.Linq;
using HealthyNutritionBot.service;
using HealthyNutritionBot.domain.interfaces;

namespace HealthyNutritionBot.domain.repositories;

public class UserRepository : IUserRepository
{
    private readonly Supabase.Client _supabase;
    private readonly FetchService _fetchService;
    private readonly InsertService _insertService;
    
    public UserRepository(Supabase.Client supabase)
    {
        _supabase = supabase;
        _fetchService = new FetchService(supabase);
        _insertService = new InsertService(supabase); // Add this line to initialize _insertService
    }

    public async Task<User> GetUserById(long id)
    {
        var users = await _fetchService.GetDataByConditionAsync<User>("users", x => x.TelegramId == id);
        return users.FirstOrDefault();
    }

    public async Task AddUserAsync(User user)
    {
        await _insertService.InsertAsync(user);
    }
}