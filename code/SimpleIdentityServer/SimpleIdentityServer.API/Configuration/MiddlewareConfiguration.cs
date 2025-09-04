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

        // Configure forwarded headers - MUST be first in pipeline for load balancer support
        if (loadBalancerConfig.EnableForwardedHeaders)
        {
            app.UseForwardedHeaders();
        }

        // Add security headers middleware - should be early in pipeline
        app.UseMiddleware<SecurityHeadersMiddleware>();

        app.UseHttpsRedirection();

        // Add CORS middleware - must be after UseHttpsRedirection
        app.UseCors("ProductionCorsPolicy");

        // Add debug logging middleware - should be very early in pipeline to capture all requests/responses
        app.UseMiddleware<DebugLoggingMiddleware>();

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
