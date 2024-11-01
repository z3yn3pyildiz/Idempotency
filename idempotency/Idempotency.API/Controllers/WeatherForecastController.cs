using Idempotency.API.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Idempotency.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly List<WeatherForecast> _weatherForecasts = new(); 

        [HttpPost("createForecast", Name = "CreateWeatherForecast")] 
        [IdempotentFullParameters(ExpireHours = 1)]
        public IActionResult Create([FromBody]CreateWeatherForecastModel model)
        {
            if (model == null)
            {
                return BadRequest("The request body cannot be empty");
            }

            var newForecast = new WeatherForecast
            {
                Date = model.Date,
                TemperatureC = model.TemperatureC,
                Summary = model.Summary
            };

            _weatherForecasts.Add(newForecast);

            return CreatedAtAction(nameof(Create), new { date = newForecast.Date }, newForecast);
        }


        [HttpPost("createByParameters", Name = "CreateByParameters")]
        [IdempotentByParameters(ExpireHours=1, SelectedParameters = new[] { "Summary" })]
        public IActionResult CreateByParameters([FromBody] CreateWeatherForecastModel model)
        {
            if (model == null)
            {
                return BadRequest("The request body cannot be empty");
            }

            var newForecast = new WeatherForecast
            {
                Date = model.Date,
                TemperatureC = model.TemperatureC,
                Summary = model.Summary
            };
            _weatherForecasts.Add(newForecast);

            return CreatedAtAction(nameof(Create), new { date = newForecast.Date }, newForecast);
        }
    }
}