using SimpleIdentityServer.API.Configuration;
using SimpleIdentityServer.Data;
using SimpleIdentityServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure secure environment settings (connection strings, certificate passwords, etc.)
ApplicationConfiguration.ConfigureSecureEnvironmentSettings(builder);

// Validate all configuration early in the startup process
ApplicationConfiguration.ValidateConfiguration(builder);

// Configure Serilog for Security Logging
SecurityLoggingConfiguration.ConfigureSerilog(builder);

// Configure ASP.NET Core logging to enable debug level for debug logging middleware
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection(AppSettingsNames.Logging));

// Configure CORS policies
ApplicationConfiguration.ConfigureCors(builder);

// Configure load balancer options and forwarded headers
var loadBalancerConfig = builder.Configuration
    .GetSection(LoadBalancerOptions.SectionName)
    .Get<LoadBalancerOptions>() ?? new LoadBalancerOptions();

ApplicationConfiguration.ConfigureForwardedHeaders(builder, loadBalancerConfig);

// Configure rate limiting
RateLimitingConfiguration.ConfigureRateLimiting(builder);

// Configure all services
ServiceConfiguration.ConfigureServices(builder);

var app = builder.Build();

// Configure the HTTP request pipeline
MiddlewareConfiguration.ConfigureMiddleware(app, loadBalancerConfig);

// Seed the database with initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    
    var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
    var scopeService = scope.ServiceProvider.GetRequiredService<IScopeService>();
    
    await clientService.SeedClientsAsync();
    await scopeService.SeedScopesAsync();
}

// Start the log cleanup service
SecurityLoggingConfiguration.StartLogCleanupService(app);

app.Run();
