using HealthyNutritionBot.domain.entities;

namespace HealthyNutritionBot.domain.repositories;

public class UserRepository
{
    public short GetId(User user)
    {
        return user.TelegramId;
    }
    
    public string GetName(User user)
    {
        return user.name;
    }
    public string GetLastname(User user)
    {
        return user.lastname;
    }
    public bool IsActive(User user)
    {
        return user.isActive;
    }
    
    private readonly Supabase.Client _supabase;

    public async Task AddUserAsync(User user)
    {
        await _supabase.From<User>().Insert(user);
    }
    
    
}