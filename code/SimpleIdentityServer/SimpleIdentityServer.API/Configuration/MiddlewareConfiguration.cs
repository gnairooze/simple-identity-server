using SimpleIdentityServer.API.Middleware;

namespace SimpleIdentityServer.API.Configuration;

public static class MiddlewareConfiguration
{
    public static void ConfigureMiddleware(WebApplication app, LoadBalancerOptions loadBalancerConfig)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Add debug logging middleware - MUST be first to capture original headers before UseForwardedHeaders processes them
        app.UseMiddleware<DebugLoggingMiddleware>();

        // Configure forwarded headers - processes X-Forwarded-For and removes it from headers collection
        if (loadBalancerConfig.EnableForwardedHeaders)
        {
            app.UseForwardedHeaders();
        }

        // Add security headers middleware - should be early in pipeline
        app.UseMiddleware<SecurityHeadersMiddleware>();

        app.UseHttpsRedirection();

        // Add CORS middleware - must be after UseHttpsRedirection
        app.UseCors("ProductionCorsPolicy");

        // Add security monitoring middleware - should be early in pipeline
        app.UseMiddleware<SecurityMonitoringMiddleware>();

        // Add rate limiting middleware - must be before authentication
        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        // Add response filtering middleware for field-level authorization
        app.UseMiddleware<ResponseFilteringMiddleware>();

        app.MapControllers();
    }
}
