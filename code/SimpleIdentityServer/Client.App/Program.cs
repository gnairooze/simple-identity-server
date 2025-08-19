using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Client.App;

class Program
{
    private static readonly HttpClient httpClient = CreateHttpClient();
    private const string IdentityServerUrl = "https://localhost:7443";
    private const string ResourceApiUrl = "https://localhost:7093"; // Resource API URL
    private const string ClientId = "service-api";
    private const string ClientSecret = "supersecret";

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler()
        {
            // Bypass SSL certificate validation in development
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        
        return new HttpClient(handler);
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine("Weather Forecast Client Application");
        Console.WriteLine("===================================");

        try
        {
            // Step 1: Get access token
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Failed to obtain access token.");
                return;
            }

            Console.WriteLine($"Access Token obtained: {token[..50]}...");
            Console.WriteLine();

            // Step 2: Use token to access protected WeatherForecast endpoint
            await AccessWeatherForecastAsync(token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static async Task<string?> GetAccessTokenAsync()
    {
        Console.WriteLine("1. Requesting access token from Identity Server...");

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("client_secret", ClientSecret)/*,
            new KeyValuePair<string, string>("scope", "api1.read")*/
        });

        try
        {
            var response = await httpClient.PostAsync($"{IdentityServerUrl}/connect/token", tokenRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Token Response: {content}");
                
                // Parse the JSON response to extract the access_token
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
                if (tokenResponse.TryGetProperty("access_token", out var accessToken))
                {
                    return accessToken.GetString();
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Token request failed: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token request exception: {ex.Message}");
        }

        return null;
    }

    static async Task AccessWeatherForecastAsync(string token)
    {
        Console.WriteLine("2. Accessing protected WeatherForecast endpoint...");

        // Set the authorization header with the Bearer token
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await httpClient.GetAsync($"{ResourceApiUrl}/WeatherForecast");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("SUCCESS: Weather forecast data retrieved:");
                Console.WriteLine("=========================================");

                // Pretty print the JSON response
                var weatherData = JsonSerializer.Deserialize<JsonElement>(content);
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.WriteLine(JsonSerializer.Serialize(weatherData, options));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Weather forecast request failed: {response.StatusCode} - {errorContent}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("The token might be invalid or the endpoint requires different scopes.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Weather forecast request exception: {ex.Message}");

            //write the whole exception object to the console
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            // Clear authorization header
            httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
