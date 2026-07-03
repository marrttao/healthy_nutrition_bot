using System;
using System.Threading.Tasks;
using Quartz;
using Microsoft.EntityFrameworkCore; // Обязательно для ExecuteUpdateAsync
using HealthyNutritionBot.Data; // Пространство имен вашего AppDbContext

namespace HealthyNutritionBot;

[DisallowConcurrentExecution]
public class ResetDailyGoals : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            // using гарантирует правильное и безопасное закрытие соединения с БД, 
            // когда задача Quartz завершится
            using var dbContext = new AppDbContext();

            // Массовое обновление: EF Core сгенерирует один запрос вида 
            // UPDATE daily_norm SET calories_today = 0, protein_today = 0 ...
            int updatedRows = await dbContext.DailyNorms
                .ExecuteUpdateAsync(s => s
                    .SetProperty(dn => dn.CaloriesToday, 0)
                    .SetProperty(dn => dn.ProteinsToday, 0)
                    .SetProperty(dn => dn.FatsToday, 0)
                    .SetProperty(dn => dn.CarbsToday, 0));

            Console.WriteLine($"Таблица daily_norm успешно сброшена. Обновлено записей: {updatedRows} в 00:00 EEST");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при очистке таблицы daily_norm: {ex.Message}");
        }
    }
}