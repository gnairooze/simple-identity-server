using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SimpleIdentityServer.API.Data;

namespace SampleSPA;

/// <summary>
/// Helper class to programmatically create test users for SampleSPA
/// Run this during Identity Server startup or as a console utility
/// </summary>
public static class TestUserSeeder
{
    /// <summary>
    /// Default test user credentials
    /// </summary>
    public static class TestCredentials
    {
        // Standard Test User
        public const string TestUserEmail = "testuser@example.com";
        public const string TestUserPassword = "Test@1234";
        public const string TestUserFirstName = "Test";
        public const string TestUserLastName = "User";

        // Admin Test User
        public const string AdminUserEmail = "admin@example.com";
        public const string AdminUserPassword = "Test@1234";
        public const string AdminUserFirstName = "Admin";
        public const string AdminUserLastName = "User";
    }

    /// <summary>
    /// Creates the standard test user if it doesn't exist
    /// </summary>
    public static async Task<IdentityResult?> CreateTestUserAsync(UserManager<ApplicationUser> userManager)
    {
        return await CreateUserAsync(
            userManager,
            TestCredentials.TestUserEmail,
            TestCredentials.TestUserPassword,
            TestCredentials.TestUserFirstName,
            TestCredentials.TestUserLastName
        );
    }

    /// <summary>
    /// Creates the admin test user if it doesn't exist
    /// </summary>
    public static async Task<IdentityResult?> CreateAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        return await CreateUserAsync(
            userManager,
            TestCredentials.AdminUserEmail,
            TestCredentials.AdminUserPassword,
            TestCredentials.AdminUserFirstName,
            TestCredentials.AdminUserLastName
        );
    }

    /// <summary>
    /// Creates a user with the specified credentials
    /// </summary>
    public static async Task<IdentityResult?> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string firstName,
        string lastName)
    {
        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            Console.WriteLine($"User '{email}' already exists.");
            return null;
        }

        // Create the user
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true, // Set to true so user can login immediately
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            Console.WriteLine($"✓ User '{email}' created successfully!");
        }
        else
        {
            Console.WriteLine($"✗ Failed to create user '{email}':");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  - {error.Description}");
            }
        }

        return result;
    }

    /// <summary>
    /// Seeds all test users
    /// </summary>
    public static async Task SeedAllTestUsersAsync(UserManager<ApplicationUser> userManager)
    {
        Console.WriteLine("Seeding test users...");
        Console.WriteLine();

        await CreateTestUserAsync(userManager);
        await CreateAdminUserAsync(userManager);

        Console.WriteLine();
        Console.WriteLine("=========================================");
        Console.WriteLine("Test Users Ready!");
        Console.WriteLine("=========================================");
        Console.WriteLine();
        Console.WriteLine("You can login with these credentials:");
        Console.WriteLine();
        Console.WriteLine("  Standard User:");
        Console.WriteLine($"    Email:    {TestCredentials.TestUserEmail}");
        Console.WriteLine($"    Password: {TestCredentials.TestUserPassword}");
        Console.WriteLine();
        Console.WriteLine("  Admin User:");
        Console.WriteLine($"    Email:    {TestCredentials.AdminUserEmail}");
        Console.WriteLine($"    Password: {TestCredentials.AdminUserPassword}");
        Console.WriteLine();
        Console.WriteLine("=========================================");
    }

    /// <summary>
    /// Seeds test users using a service provider
    /// Call this from Program.cs during startup
    /// </summary>
    public static async Task SeedTestUsersAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await SeedAllTestUsersAsync(userManager);
    }
}

// =============================================================================
// INTEGRATION INSTRUCTIONS
// =============================================================================
// 
// Add this to your Identity Server's Program.cs after the existing seed calls:
// 
// var app = builder.Build();
// 
// // Seed the database with initial data
// using (var scope = app.Services.CreateScope())
// {
//     var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//     await context.Database.EnsureCreatedAsync();
//     
//     var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
//     var scopeService = scope.ServiceProvider.GetRequiredService<IScopeService>();
//     
//     await clientService.SeedClientsAsync();
//     await scopeService.SeedScopesAsync();
//     
//     // ADD THIS: Seed test users
//     var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//     await TestUserSeeder.SeedAllTestUsersAsync(userManager);
// }
// 
// app.Run();
// 
// =============================================================================

// =============================================================================
// ALTERNATIVE: Generate Password Hash for SQL Script
// =============================================================================
// 
// If you need to generate a new password hash for use in SQL scripts,
// you can use this utility method:
// 
// public static string GeneratePasswordHash(string password)
// {
//     var hasher = new PasswordHasher<ApplicationUser>();
//     return hasher.HashPassword(null!, password);
// }
// 
// Example usage:
//   var hash = GeneratePasswordHash("Test@1234");
//   Console.WriteLine($"Password hash: {hash}");
// 
// Then use this hash in your SQL INSERT statement.
// 
// =============================================================================

