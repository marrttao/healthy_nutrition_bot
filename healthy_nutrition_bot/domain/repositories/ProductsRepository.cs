using HealthyNutritionBot.domain.entities;
using HealthyNutritionBot.domain.interfaces;
using HealthyNutritionBot.service;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthyNutritionBot.domain.repositories
{
    public class ProductsRepository : IProductsRepository
    {
        private readonly Supabase.Client _supabase;
        private readonly FetchService _fetchService;
        private readonly InsertService _insertService;
        private readonly UpdateService _updateService;

        public ProductsRepository(Supabase.Client supabase)
        {
            _supabase = supabase;
            _fetchService = new FetchService(supabase);
            _insertService = new InsertService(supabase);
            _updateService = new UpdateService(supabase);
        }

        public async Task<Products?> GetProductByIdAsync(short id)
        {
            var products = await _fetchService.GetDataByConditionAsync<Products>("products", x => x.id == id);
            return products.FirstOrDefault();
        }

        public async Task<IEnumerable<Products>> GetAllProductsAsync()
        {
            return await _fetchService.GetDataAsync<Products>("products");
        }

        public async Task AddProductAsync(Products product)
        {
            await _insertService.InsertAsync(product);
        }

        public async Task UpdateProductAsync(Products product)
        {
            await _updateService.UpdateAsync(product);
        }

    }
}

