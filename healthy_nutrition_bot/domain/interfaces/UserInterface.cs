using HealthyNutritionBot.domain.entities;
namespace HealthyNutritionBot.domain.interfaces;

public interface UserInterface
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
}

