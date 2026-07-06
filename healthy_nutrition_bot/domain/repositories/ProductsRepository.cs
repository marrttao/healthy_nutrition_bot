using HealthyNutritionBot.domain.entities;
using HealthyNutritionBot.domain.interfaces;
using HealthyNutritionBot.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthyNutritionBot.domain.repositories
{
    public class ProductsRepository : IProductsRepository
    {
        private readonly AppDbContext _context;

        public ProductsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Products?> GetProductByIdAsync(short id)
        {
            // Используем FirstOrDefaultAsync из EF Core. 
            // Убедитесь, что в модели Products свойство называется Id (с большой буквы), если вы его переименовали.
            return await _context.Products.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<Products>> GetAllProductsAsync()
        {
            // Возвращаем весь список товаров
            return await _context.Products.ToListAsync();
        }

        public async Task AddProductAsync(Products product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(Products product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }
    }
}