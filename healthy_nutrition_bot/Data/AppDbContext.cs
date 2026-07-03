using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using HealthyNutritionBot.domain.entities; 
using HealthyNutritionBot.service.TokenReader;

namespace HealthyNutritionBot.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<DailyNorm> DailyNorms { get; set; }
    public DbSet<StatsOfUsers> StatsOfUsers { get; set; }
    public DbSet<Products> Products { get; set; }
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Берем строку из переменных среды ОС. 
            // Мы не ищем файл .env, а доверяем тому, что переменная УЖЕ есть в системе.
            string connectionString = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("Ошибка: Переменная среды SUPABASE_CONNECTION_STRING не установлена!");
            }

            optionsBuilder.UseNpgsql(connectionString);
        }
    }
}