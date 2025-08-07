using System.Net.Http.Headers;
using System.Text;

namespace SampleClient;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();
    private const string IdentityServerUrl = "https://localhost:7001";
    private const string ClientId = "service-api";
    private const string ClientSecret = "supersecret";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Simple Identity Server - Sample Client");
        Console.WriteLine("=====================================");

        try
        {
            // Step 1: Get access token using client credentials flow
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Failed to obtain access token.");
                return;
            }

            Console.WriteLine($"Access Token: {token}");
            Console.WriteLine();

            // Step 2: Use the token to access protected resources
            await UseAccessTokenAsync(token);

            // Step 3: Decode and display token information
            await DecodeTokenAsync(token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static async Task<string> GetAccessTokenAsync()
    {
        Console.WriteLine("1. Requesting access token...");

        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("client_secret", ClientSecret),
            new KeyValuePair<string, string>("scope", "api1.read api1.write")
        });

        var response = await httpClient.PostAsync($"{IdentityServerUrl}/connect/token", tokenRequest);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Token Response: {content}");
            
            // Parse the JSON response to extract the access_token
            // In a real application, you'd use a JSON library like Newtonsoft.Json
            var tokenStart = content.IndexOf("\"access_token\":\"") + 16;
            var tokenEnd = content.IndexOf("\"", tokenStart);
            return content.Substring(tokenStart, tokenEnd - tokenStart);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {response.StatusCode} - {errorContent}");
            return null;
        }
    }

    static async Task UseAccessTokenAsync(string token)
    {
        Console.WriteLine("\n2. Using access token to access protected resources...");

        // Example: Call the client management API
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        try
        {
            var response = await httpClient.GetAsync($"{IdentityServerUrl}/api/client");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Protected Resource Response: {content}");
            }
            else
            {
                Console.WriteLine($"Error accessing protected resource: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing protected resource: {ex.Message}");
        }
    }

    static async Task DecodeTokenAsync(string token)
    {
        Console.WriteLine("\n3. Token Information:");
        Console.WriteLine("=====================");

        // In a real application, you'd use a JWT library to decode the token
        // For this example, we'll just show the token structure
        var parts = token.Split('.');
        if (parts.Length == 3)
        {
            Console.WriteLine("JWT Token Structure:");
            Console.WriteLine($"Header: {parts[0]}");
            Console.WriteLine($"Payload: {parts[1]}");
            Console.WriteLine($"Signature: {parts[2]}");
            
            // Decode the payload (base64url decode)
            var payload = parts[1];
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 0: break;
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            
            try
            {
                var jsonBytes = Convert.FromBase64String(payload);
                var json = Encoding.UTF8.GetString(jsonBytes);
                Console.WriteLine($"\nDecoded Payload: {json}");
            }
            catch
            {
                Console.WriteLine("Could not decode token payload");
            }
        }
    }
} 