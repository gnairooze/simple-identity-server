using FluentAssertions;
using SimpleIdentityServer.API.Test.Infrastructure;
using System.Diagnostics;
using System.Net;
using Xunit;

namespace SimpleIdentityServer.API.Test.Performance;

public class LoadTests : TestBase
{
    public LoadTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Token_ConcurrentRequests_ShouldHandleLoad()
    {
        // Arrange
        const int concurrentRequests = 50;
        const int maxAcceptableResponseTimeMs = 5000; // 5 seconds max

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        var tasks = new List<Task<(HttpResponseMessage Response, TimeSpan Duration)>>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(MeasureRequestTime(async () => 
                await Client.PostAsync("/connect/token", CreateFormContent(tokenRequest))));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var successfulResponses = results.Where(r => r.Response.IsSuccessStatusCode).ToList();
        var failedResponses = results.Where(r => !r.Response.IsSuccessStatusCode).ToList();

        // At least 90% of requests should succeed
        successfulResponses.Count.Should().BeGreaterThanOrEqualTo((int)(concurrentRequests * 0.9),
            $"Expected at least 90% success rate, but got {successfulResponses.Count}/{concurrentRequests}");

        // Average response time should be reasonable
        var averageResponseTime = results.Average(r => r.Duration.TotalMilliseconds);
        averageResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs,
            $"Average response time {averageResponseTime}ms exceeds acceptable threshold");

        // Total test time should be reasonable (concurrent execution)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxAcceptableResponseTimeMs * 2,
            "Total test execution time indicates requests were not processed concurrently");

        // Log performance metrics for analysis
        Console.WriteLine($"Load Test Results:");
        Console.WriteLine($"- Concurrent Requests: {concurrentRequests}");
        Console.WriteLine($"- Successful: {successfulResponses.Count}");
        Console.WriteLine($"- Failed: {failedResponses.Count}");
        Console.WriteLine($"- Average Response Time: {averageResponseTime:F2}ms");
        Console.WriteLine($"- Min Response Time: {results.Min(r => r.Duration.TotalMilliseconds):F2}ms");
        Console.WriteLine($"- Max Response Time: {results.Max(r => r.Duration.TotalMilliseconds):F2}ms");
        Console.WriteLine($"- Total Execution Time: {stopwatch.ElapsedMilliseconds}ms");

        // Cleanup
        foreach (var result in results)
        {
            result.Response.Dispose();
        }
    }

    [Fact]
    public async Task Introspect_ConcurrentRequests_ShouldHandleLoad()
    {
        // Arrange
        const int concurrentRequests = 50;
        const int maxAcceptableResponseTimeMs = 5000; // 5 seconds max

        // First get a valid token
        var accessToken = await GetAccessTokenAsync("service-api", "supersecret");

        var introspectRequest = new Dictionary<string, string>
        {
            ["token"] = accessToken,
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        var tasks = new List<Task<(HttpResponseMessage Response, TimeSpan Duration)>>();
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(MeasureRequestTime(async () => 
                await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest))));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var successfulResponses = results.Where(r => r.Response.IsSuccessStatusCode).ToList();
        var failedResponses = results.Where(r => !r.Response.IsSuccessStatusCode).ToList();

        // At least 90% of requests should succeed
        successfulResponses.Count.Should().BeGreaterThanOrEqualTo((int)(concurrentRequests * 0.9),
            $"Expected at least 90% success rate, but got {successfulResponses.Count}/{concurrentRequests}");

        // Average response time should be reasonable
        var averageResponseTime = results.Average(r => r.Duration.TotalMilliseconds);
        averageResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs,
            $"Average response time {averageResponseTime}ms exceeds acceptable threshold");

        // All successful responses should show token as active
        foreach (var successfulResponse in successfulResponses)
        {
            var introspectionResult = await DeserializeResponseAsync<IntrospectionResponse>(successfulResponse.Response);
            introspectionResult.Should().NotBeNull();
            introspectionResult!.Active.Should().BeTrue();
        }

        // Log performance metrics
        Console.WriteLine($"Introspect Load Test Results:");
        Console.WriteLine($"- Concurrent Requests: {concurrentRequests}");
        Console.WriteLine($"- Successful: {successfulResponses.Count}");
        Console.WriteLine($"- Failed: {failedResponses.Count}");
        Console.WriteLine($"- Average Response Time: {averageResponseTime:F2}ms");
        Console.WriteLine($"- Min Response Time: {results.Min(r => r.Duration.TotalMilliseconds):F2}ms");
        Console.WriteLine($"- Max Response Time: {results.Max(r => r.Duration.TotalMilliseconds):F2}ms");
        Console.WriteLine($"- Total Execution Time: {stopwatch.ElapsedMilliseconds}ms");

        // Cleanup
        foreach (var result in results)
        {
            result.Response.Dispose();
        }
    }

    [Fact]
    public async Task Token_SequentialRequests_ShouldMaintainPerformance()
    {
        // Arrange
        const int sequentialRequests = 20;
        const int maxAcceptableResponseTimeMs = 2000; // 2 seconds max per request

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "service-api",
            ["client_secret"] = "supersecret"
        };

        var responseTimes = new List<TimeSpan>();

        // Act
        for (int i = 0; i < sequentialRequests; i++)
        {
            var (response, duration) = await MeasureRequestTime(async () => 
                await Client.PostAsync("/connect/token", CreateFormContent(tokenRequest)));

            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Request {i + 1} should succeed");
            responseTimes.Add(duration);
            
            response.Dispose();
        }

        // Assert
        var averageResponseTime = responseTimes.Average(t => t.TotalMilliseconds);
        var maxResponseTime = responseTimes.Max(t => t.TotalMilliseconds);

        averageResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs,
            $"Average response time {averageResponseTime}ms exceeds acceptable threshold");

        maxResponseTime.Should().BeLessThan(maxAcceptableResponseTimeMs * 2,
            $"Max response time {maxResponseTime}ms is too high");

        // Performance should not degrade significantly over time
        var firstHalfAvg = responseTimes.Take(sequentialRequests / 2).Average(t => t.TotalMilliseconds);
        var secondHalfAvg = responseTimes.Skip(sequentialRequests / 2).Average(t => t.TotalMilliseconds);

        // Second half should not be more than 50% slower than first half
        secondHalfAvg.Should().BeLessThan(firstHalfAvg * 1.5,
            "Performance should not degrade significantly over sequential requests");

        Console.WriteLine($"Sequential Test Results:");
        Console.WriteLine($"- Sequential Requests: {sequentialRequests}");
        Console.WriteLine($"- Average Response Time: {averageResponseTime:F2}ms");
        Console.WriteLine($"- Max Response Time: {maxResponseTime:F2}ms");
        Console.WriteLine($"- First Half Average: {firstHalfAvg:F2}ms");
        Console.WriteLine($"- Second Half Average: {secondHalfAvg:F2}ms");
    }

    [Fact]
    public async Task TokenAndIntrospect_MixedLoad_ShouldHandleEfficiently()
    {
        // Arrange
        const int totalRequests = 40;
        const int tokenRequests = 20;

        // First, get some tokens for introspection
        var tokens = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var token = await GetAccessTokenAsync("service-api", "supersecret");
            tokens.Add(token);
        }

        var tasks = new List<Task<(string RequestType, HttpResponseMessage Response, TimeSpan Duration)>>();
        var random = new Random();

        // Act - Mix token and introspect requests
        for (int i = 0; i < totalRequests; i++)
        {
            if (i < tokenRequests)
            {
                // Token request
                var tokenRequest = new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = "service-api",
                    ["client_secret"] = "supersecret"
                };

                tasks.Add(MeasureRequestTimeWithType("Token", async () => 
                    await Client.PostAsync("/connect/token", CreateFormContent(tokenRequest))));
            }
            else
            {
                // Introspect request
                var randomToken = tokens[random.Next(tokens.Count)];
                var introspectRequest = new Dictionary<string, string>
                {
                    ["token"] = randomToken,
                    ["client_id"] = "service-api",
                    ["client_secret"] = "supersecret"
                };

                tasks.Add(MeasureRequestTimeWithType("Introspect", async () => 
                    await Client.PostAsync("/connect/introspect", CreateFormContent(introspectRequest))));
            }
        }

        // Shuffle tasks to simulate real mixed load
        for (int i = tasks.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (tasks[i], tasks[j]) = (tasks[j], tasks[i]);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var tokenResults = results.Where(r => r.RequestType == "Token").ToList();
        var introspectResults = results.Where(r => r.RequestType == "Introspect").ToList();

        // All requests should succeed
        tokenResults.Should().AllSatisfy(r => r.Response.IsSuccessStatusCode.Should().BeTrue());
        introspectResults.Should().AllSatisfy(r => r.Response.IsSuccessStatusCode.Should().BeTrue());

        // Performance should be reasonable for both types
        var tokenAvgTime = tokenResults.Average(r => r.Duration.TotalMilliseconds);
        var introspectAvgTime = introspectResults.Average(r => r.Duration.TotalMilliseconds);

        tokenAvgTime.Should().BeLessThan(3000, "Token requests should complete within 3 seconds on average");
        introspectAvgTime.Should().BeLessThan(3000, "Introspect requests should complete within 3 seconds on average");

        Console.WriteLine($"Mixed Load Test Results:");
        Console.WriteLine($"- Token Requests: {tokenResults.Count}");
        Console.WriteLine($"- Introspect Requests: {introspectResults.Count}");
        Console.WriteLine($"- Token Average Time: {tokenAvgTime:F2}ms");
        Console.WriteLine($"- Introspect Average Time: {introspectAvgTime:F2}ms");

        // Cleanup
        foreach (var result in results)
        {
            result.Response.Dispose();
        }
    }

    private async Task<(HttpResponseMessage Response, TimeSpan Duration)> MeasureRequestTime(
        Func<Task<HttpResponseMessage>> requestFunc)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await requestFunc();
        stopwatch.Stop();
        return (response, stopwatch.Elapsed);
    }

    private async Task<(string RequestType, HttpResponseMessage Response, TimeSpan Duration)> MeasureRequestTimeWithType(
        string requestType, Func<Task<HttpResponseMessage>> requestFunc)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await requestFunc();
        stopwatch.Stop();
        return (requestType, response, stopwatch.Elapsed);
    }
}
