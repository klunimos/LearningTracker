using LearningTracker.Api.Logic.DTO.WeatherForecast;

namespace LearningTracker.Api.Logic.Services;

public interface IWeatherForecastService
{
    IList<WeatherForecastResponse> GetForecasts();
}

public class WeatherForecastService : IWeatherForecastService
{
    public IList<WeatherForecastResponse> GetForecasts()
    {
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild",
            "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        var items = new List<WeatherForecastResponse>();

        for (var index = 1; index <= 5; index++)
        {
            var forecast = new WeatherForecastResponse
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = summaries[Random.Shared.Next(summaries.Length)]
            };

            items.Add(forecast);
        }

        return items;
    }
}
