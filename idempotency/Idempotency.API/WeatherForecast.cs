namespace Idempotency.API
{
    [Serializable]
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public string? Summary { get; set; }
    }

    [Serializable]
    public class CreateWeatherForecastModel
    {
        public DateOnly Date { get; set; }
        public int TemperatureC { get; set; }
        public string Summary { get; set; }
    }
}