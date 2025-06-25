namespace healthy_nutrition_bot.Core.controllers;

using Microsoft.AspNetCore.Mvc;
using UsdaFoodApi.Models;
using healthy_nutrition_bot.Core.service;

[ApiController]
[Route("api/[controller]")]
public class FoodController : ControllerBase
{
    private readonly UsdaService _usdaService;

    public FoodController(UsdaService usdaService)
    {
        _usdaService = usdaService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<FoodNutrientResponse>>> SearchFood([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query parameter is required.");
        }

        try
        {
            var results = await _usdaService.GetFoodInfoAsync(query);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }
}

