namespace Microsoft.AzureCore.ReadyToDeploy.ViraUI.Server.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;

    using Swashbuckle.AspNetCore.Annotations;

    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        /// <summary>
        /// Determine weather in my location.
        /// </summary>
        /// <param name="request">The weather request containing location and unit.</param>
        /// <returns>The weather information for the specified location.</returns>
        [HttpPost("get_weather")]
        [SwaggerOperation(OperationId = "get_weather", Summary = "Determine weather in my location")]
        [ProducesResponseType(typeof(GetWeatherResponse), 200)]
        public async Task<IActionResult> GetWeather([FromBody] GetWeatherRequest request)
        {
            // Validate the request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Simulate fetching weather data (replace with actual logic)
            var response = new GetWeatherResponse
            {
                Location = request.Location,
                Temperature = request.Unit.ToLower() == "c" ? "20°C" : "68°F",
                Description = "Sunny"
            };

            // Simulate async operation
            await Task.Delay(100); // Remove in production

            return Ok(response);
        }
    }

    /// <summary>
    /// Represents the response containing weather information.
    /// </summary>
    public class GetWeatherResponse
    {
        /// <summary>
        /// The location for which the weather is provided.
        /// </summary>
        public required string Location { get; set; }

        /// <summary>
        /// The current temperature.
        /// </summary>
        public required string Temperature { get; set; }

        /// <summary>
        /// A brief description of the current weather.
        /// </summary>
        public required string Description { get; set; }
    }

    /// <summary>
    /// Represents the request for getting weather information.
    /// </summary>
    public class GetWeatherRequest
    {
        /// <summary>
        /// The city and state e.g. Seattle, WA
        /// </summary>
        [Required]
        public required string Location { get; set; }

        /// <summary>
        /// The unit of temperature. "c" for Celsius, "f" for Fahrenheit.
        /// </summary>
        [RegularExpression("^(c|f)$", ErrorMessage = "Unit must be either 'c' or 'f'.")]
        public string Unit { get; set; } = "f"; // Default to Fahrenheit
    }
}
