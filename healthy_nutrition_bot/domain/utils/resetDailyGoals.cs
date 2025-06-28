using System;
using System.Threading.Tasks;
using Quartz;
using HealthyNutritionBot.domain.entities;
using HealthyNutritionBot.service;

[DisallowConcurrentExecution]
public class ResetDailyGoals : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            // Создаем новый клиент Supabase для потокобезопасности
            var supabaseService = new SupabaseService();
            await supabaseService.InitializeAsync();
            var _supabase = supabaseService._supabase;
            var updateService = new UpdateService(_supabase);
            var fetchService = new FetchService(_supabase);

            // update daily_norm для всех пользователей
            var dailyNorms = await fetchService.GetDataAsync<DailyNorm>("daily_norm");
            foreach (var dailyNorm in dailyNorms)
            {
                // Сброс значений daily_norm
                dailyNorm.CaloriesToday = 0;
                dailyNorm.ProteinsToday = 0;
                dailyNorm.FatsToday = 0;
                dailyNorm.CarbsToday = 0;

                // Обновляем запись в базе данных
                await updateService.UpdateAsync(dailyNorm);
            }
            Console.WriteLine("Таблица daily_norm очищена в 00:00 EEST");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при очистке таблицы daily_norm: {ex.Message}");
        }
    }
}