// UserInterface.cs
using HealthyNutritionBot.domain.entities;
using System.Threading.Tasks;

namespace HealthyNutritionBot.domain.interfaces;

public interface IUserRepository
{
    Task<User> GetUserById(long id);
    Task AddUserAsync(User user);
}