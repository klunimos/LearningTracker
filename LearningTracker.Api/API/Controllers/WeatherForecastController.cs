using LearningTracker.Api.Logic.Services;
using Microsoft.AspNetCore.Mvc;

namespace LearningTracker.Api.API.Controllers;

public class WeatherForecastController : GlobalController
{
    private readonly IWeatherForecastService weatherForecastService;

    public WeatherForecastController(IWeatherForecastService weatherForecastService)
    {
        this.weatherForecastService = weatherForecastService;
    }

    public IActionResult Get()
    {
        var forecasts = weatherForecastService.GetForecasts();
        return Success(forecasts);
    }
}
