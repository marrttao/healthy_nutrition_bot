using System;
using System.Threading.Tasks;
using Supabase;
using HealthyNutritionBot.service;

namespace HealthyNutritionBot.service;

public class SupabaseService
{
    private readonly string _url;
    private readonly string _key;
    private readonly SupabaseOptions _options;
    public Client _supabase;

    public SupabaseService()
    {
        var tokenReader = new TokenReader.TokenReader();
        _url = tokenReader.GetSupabaseUrl();
        _key = tokenReader.GetSupabaseKey();
        _options = new SupabaseOptions
        {
            AutoConnectRealtime = true
        };
        _supabase = new Client(_url, _key, _options);
    }

    public async Task InitializeAsync()
    {
        await _supabase.InitializeAsync();
    }
}