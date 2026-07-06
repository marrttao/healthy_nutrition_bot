using HealthyNutritionBot.domain.entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthyNutritionBot.domain.interfaces
{
    public interface IProductsRepository
    {
        Task<Products?> GetProductByIdAsync(short id);
        Task<IEnumerable<Products>> GetAllProductsAsync();
        Task AddProductAsync(Products product);
        Task UpdateProductAsync(Products product);
    }
}

