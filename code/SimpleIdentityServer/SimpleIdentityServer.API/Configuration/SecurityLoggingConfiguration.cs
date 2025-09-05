using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;

namespace SimpleIdentityServer.API.Configuration;

public static class SecurityLoggingConfiguration
{
    public static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        // Get connection string from environment variables or configuration
        var connectionString = GetSecurityLogsConnectionString(builder);
        var nodeName = Environment.GetEnvironmentVariable(EnvironmentVariablesNames.NodeName) ?? Environment.MachineName;
        
        // Define custom columns for security logs
        var columnOptions = new ColumnOptions
        {
            DisableTriggers = true,
            ClusteredColumnstoreIndex = false
        };
        
        // Keep essential columns but remove Properties and MessageTemplate
        columnOptions.Store.Remove(StandardColumn.Properties);
            
        // Ensure LogEvent column is included (this contains the actual log message)
        columnOptions.Store.Add(StandardColumn.LogEvent);
        
        // Add custom columns for security data
        columnOptions.AdditionalColumns = new Collection<SqlColumn>
        {
            new SqlColumn { ColumnName = "RequestId", DataType = SqlDbType.NVarChar, DataLength = 100, AllowNull = true },
            new SqlColumn { ColumnName = "EventType", DataType = SqlDbType.NVarChar, DataLength = 50, AllowNull = true },
            new SqlColumn { ColumnName = "IpAddress", DataType = SqlDbType.NVarChar, DataLength = 45, AllowNull = true },
            new SqlColumn { ColumnName = "UserAgent", DataType = SqlDbType.NVarChar, DataLength = 500, AllowNull = true },
            new SqlColumn { ColumnName = "Path", DataType = SqlDbType.NVarChar, DataLength = 200, AllowNull = true },
            new SqlColumn { ColumnName = "Method", DataType = SqlDbType.NVarChar, DataLength = 10, AllowNull = true },
            new SqlColumn { ColumnName = "StatusCode", DataType = SqlDbType.Int, AllowNull = true },
            new SqlColumn { ColumnName = "DurationMs", DataType = SqlDbType.Float, AllowNull = true },
            new SqlColumn { ColumnName = "ClientId", DataType = SqlDbType.NVarChar, DataLength = 100, AllowNull = true },
            new SqlColumn { ColumnName = "NodeName", DataType = SqlDbType.NVarChar, DataLength = 50, AllowNull = true }
        };

        // Configure Serilog
        var loggerConfig = new LoggerConfiguration()
            .Enrich.WithProperty("NodeName", nodeName)
            .Enrich.FromLogContext()
            .MinimumLevel.Debug() // Enable debug level logging
            .WriteTo.Console(
                restrictedToMinimumLevel: builder.Environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

        // Only add SQL Server sink if we have a valid connection string
        if (!string.IsNullOrEmpty(connectionString))
        {
            loggerConfig.WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: new MSSqlServerSinkOptions
                {
                    TableName = "SecurityLogs",
                    SchemaName = "dbo",
                    AutoCreateSqlTable = true,
                    BatchPostingLimit = GetSecurityLoggingBatchLimit(builder),
                    BatchPeriod = TimeSpan.FromSeconds(GetSecurityLoggingBatchPeriod(builder))
                },
                columnOptions: columnOptions);
        }
        else
        {
            // Log warning that SQL Server logging is not available
            Console.WriteLine("WARNING: No SQL Server connection string available for security logging. Using console logging only.");
        }

        Log.Logger = loggerConfig.CreateLogger();

        // Use Serilog for ASP.NET Core logging
        builder.Host.UseSerilog();
    }

    public static void StartLogCleanupService(WebApplication app)
    {
        // Get connection string from environment variables or configuration
        var connectionString = GetSecurityLogsConnectionString(app.Configuration, app.Environment);
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        // Only start cleanup service if we have a valid connection string
        if (string.IsNullOrEmpty(connectionString))
        {
            logger.LogWarning("No SQL Server connection string available for security logs cleanup service. Cleanup service will not start.");
            return;
        }
        
        Task.Run(async () =>
        {
            while (!app.Lifetime.ApplicationStopping.IsCancellationRequested)
            {
                try
                {
                    using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                    await connection.OpenAsync();
                    
                    // Delete logs older than configured retention period
                    var retentionDays = app.Configuration.GetValue<int>(AppSettingsNames.ApplicationSecurityLoggingRetentionDays, 30);
                    var deleteCommand = $@"
                        DELETE FROM SecurityLogs 
                        WHERE TimeStamp < DATEADD(day, -{retentionDays}, GETUTCDATE())";
                        
                    using var command = new Microsoft.Data.SqlClient.SqlCommand(deleteCommand, connection);
                    var deletedRows = await command.ExecuteNonQueryAsync();
                    
                    if (deletedRows > 0)
                    {
                        logger.LogInformation("Security Log Cleanup: Deleted {DeletedRows} old security log records", deletedRows);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during security log cleanup");
                }
                
                // Run cleanup every 24 hours
                var cleanupInterval = app.Configuration.GetValue<int>(AppSettingsNames.ApplicationSecurityLoggingCleanupIntervalHours, 24);
                await Task.Delay(TimeSpan.FromHours(cleanupInterval), app.Lifetime.ApplicationStopping);
            }
        });
    }

    private static string? GetSecurityLogsConnectionString(WebApplicationBuilder builder)
    {
        return GetSecurityLogsConnectionString(builder.Configuration, builder.Environment);
    }

    private static string? GetSecurityLogsConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString(AppSettingsNames.SecurityLogsConnection);
        
        // If connection string is empty, try environment variables
        if (string.IsNullOrEmpty(connectionString))
        {
            var securityLogsConnection = Environment.GetEnvironmentVariable(EnvironmentVariablesNames.SecurityLogsConnectionString);
            if (!string.IsNullOrEmpty(securityLogsConnection))
            {
                connectionString = securityLogsConnection;
            }
            else if (environment.IsDevelopment())
            {
                // Fallback for development
                connectionString = "Server=localhost;Database=SimpleIdentityServer_SecurityLogs_Dev;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true";
            }
            else
            {
                // In production, we'll configure console-only logging if no connection string is available
                connectionString = null;
            }
        }
        
        return connectionString;
    }

    private static int GetSecurityLoggingBatchLimit(WebApplicationBuilder builder)
    {
        return builder.Configuration.GetValue<int>(AppSettingsNames.ApplicationSecurityLoggingBatchPostingLimit, 50);
    }

    private static int GetSecurityLoggingBatchPeriod(WebApplicationBuilder builder)
    {
        return builder.Configuration.GetValue<int>(AppSettingsNames.ApplicationSecurityLoggingBatchPeriodSeconds, 10);
    }
}
