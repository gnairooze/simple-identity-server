using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Resource.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        [Authorize(Policy = "RequireApi1Read")]
        public IEnumerable<WeatherForecast> Get()
        {
            var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

            // Apply field-level authorization based on user claims
            return FilterForecastsBasedOnClaims(forecasts);
        }

        [HttpGet("detailed")]
        [Authorize(Policy = "RequireApi1Read")]
        public IEnumerable<object> GetDetailed()
        {
            var forecasts = Enumerable.Range(1, 5).Select(index => new
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                TemperatureF = 32 + (int)(Random.Shared.Next(-20, 55) / 0.5556),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)],
                Humidity = Random.Shared.Next(30, 90),
                Pressure = Random.Shared.Next(980, 1040),
                Location = "Sample City",
                InternalId = Guid.NewGuid() // Sensitive internal identifier
            })
            .ToArray();

            // Apply field-level authorization based on user claims
            return FilterDetailedForecastsBasedOnClaims(forecasts);
        }

        private IEnumerable<WeatherForecast> FilterForecastsBasedOnClaims(WeatherForecast[] forecasts)
        {
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var clientId = User.FindFirst("client_id")?.Value;

            // Service and admin clients get full data
            if (userRoles.Contains("service") || userRoles.Contains("admin") || clientId == "service-api")
            {
                return forecasts;
            }

            // Regular users get limited data (no temperature details)
            return forecasts.Select(f => new WeatherForecast
            {
                Date = f.Date,
                Summary = f.Summary,
                TemperatureC = 0, // Filtered out for regular users
            });
        }

        private IEnumerable<object> FilterDetailedForecastsBasedOnClaims(object[] forecasts)
        {
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var clientId = User.FindFirst("client_id")?.Value;

            return forecasts.Select(forecast =>
            {
                var forecastType = forecast.GetType();
                var properties = forecastType.GetProperties();
                var filteredData = new Dictionary<string, object?>();

                foreach (var prop in properties)
                {
                    var value = prop.GetValue(forecast);
                    
                    // Apply field-level filtering
                    if (ShouldIncludeProperty(prop.Name, userRoles, clientId))
                    {
                        filteredData[prop.Name.ToLower()] = value;
                    }
                }

                return filteredData;
            });
        }

        private static bool ShouldIncludeProperty(string propertyName, List<string> userRoles, string? clientId)
        {
            // Define sensitive fields that require elevated permissions
            var sensitiveFields = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "TemperatureC", new[] { "service", "admin" } },
                { "TemperatureF", new[] { "service", "admin" } },
                { "Humidity", new[] { "service", "admin" } },
                { "Pressure", new[] { "service", "admin" } },
                { "InternalId", new[] { "admin" } }, // Only admin can see internal IDs
                // Basic fields available to all authenticated users
                { "Date", new[] { "web_user", "mobile_user", "service", "admin" } },
                { "Summary", new[] { "web_user", "mobile_user", "service", "admin" } },
                { "Location", new[] { "web_user", "mobile_user", "service", "admin" } }
            };

            // Allow all properties if no specific rules defined
            if (!sensitiveFields.ContainsKey(propertyName))
            {
                return true;
            }

            var allowedRoles = sensitiveFields[propertyName];
            
            // Check if user has required roles or is a trusted client
            return userRoles.Any(role => allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase)) ||
                   (clientId != null && allowedRoles.Contains(clientId, StringComparer.OrdinalIgnoreCase));
        }
    }
}
