
using SimpleIdentityServer.CLI.Business;

// Run all tests
Console.WriteLine("Starting CLI Tests...\n");

await Test01_ListApplications();
await Test02_ListScopes();
await Test03_AddApplication();
await Test04_GetApplication();
await Test05_UpdateApplication();
await Test06_DeleteApplication();
await Test07_AddScope();
await Test08_GetScope();
await Test09_UpdateScope();
await Test10_DeleteScope();

Console.WriteLine("\nAll tests completed!");

// Test 01: List all applications
async Task Test01_ListApplications()
{
    Console.WriteLine("=== Test01: List Applications ===");
    try
    {
        var (appMgr, _) = CreateManagers();
        await appMgr.ListApplications();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Test01 Error: {ex.Message}");
    }
    Console.WriteLine();
}

// Test 02: List all scopes
async Task Test02_ListScopes()
{
    Console.WriteLine("=== Test02: List Scopes ===");
    try
    {
        var (_, scpMgr) = CreateManagers();
        await scpMgr.ListScopes();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Test02 Error: {ex.Message}");
    }
    Console.WriteLine();
}

// Test 03: Add new application
async Task Test03_AddApplication()
{
    Console.WriteLine("=== Test03: Add Application ===");
    try
    {
        var (appMgr, _) = CreateManagers();
        await appMgr.AddApplication(
            "test-client-01", 
            "test-secret-01", 
            "Test Client 01", 
            new[] { "ept:token", "ept:introspection", "gt:client_credentials", "scp:email", "scp:profile" }
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Test03 Error: {ex.Message}");
    }
    Console.WriteLine();
}

// Test 04: Get specific application
async Task Test04_GetApplication()
{
    Console.WriteLine("=== Test04: Get Application ===");
    try
    {
        var (appMgr, _) = CreateManagers();
        
        // Try to get the application we just created
        await appMgr.GetApplication("test-client-01");
        
        // Also try to get an existing application
        await appMgr.GetApplication("service-api");
        
        // Try to get a non-existent application
        await appMgr.GetApplication("non-existent-client");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Test04 Error: {ex.Message}");
    }
    Console.WriteLine();
}

// Test 05: Update application
async Task Test05_UpdateApplication()
{
    Console.WriteLine("=== Test05: Update Application ===");
    try
    {
        var (appMgr, _) = CreateManagers();
        
        // Update the test application we created
        await appMgr.UpdateApplication(
            "test-client-01",
            "updated-secret-01",
            "Updated Test Client 01",
            new[] { "ept:token", "ept:introspection", "gt:client_credentials", "scp:email", "scp:profile", "scp:api1.read" }
        );
        
        // Try to update a non-existent application
        await appMgr.UpdateApplication("non-existent-client", null, "Won't work", null);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Test05 Error: {ex.Message}");
    }
    Console.WriteLine();
}

// Test 06: Delete application
async Task Test06_DeleteApplication()
{
    Console.WriteLine("=== Test06: Delete Application ===");
    try
    {
        var (appMgr, _) = CreateManagers();
        
        // Delete the test application we created
        await appMgr.DeleteApplication("test-client-01");
        
        // Try to delete a non-existent application
        await appMgr.DeleteApplication("non-existent-client");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Test06 Error: {ex.Message}");
    }
    Console.WriteLine();
}

// Test 07: Add new scope
async Task Test07_AddScope()
{
    Console.WriteLine("=== Test07: Add Scope ===");
    try
    {
        var (_, scpMgr) = CreateManagers();
        await scpMgr.AddScope(
            "test-scope-01",
            "Test Scope 01",
            new[] { "test-api", "test-resource" }
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Test07 Error: {ex.Message}");
    }
    Console.WriteLine();
}

// Test 08: Get specific scope
async Task Test08_GetScope()
{
    Console.WriteLine("=== Test08: Get Scope ===");
    try
    {
        var (_, scpMgr) = CreateManagers();
        
        // Try to get the scope we just created
        await scpMgr.GetScope("test-scope-01");
        
        // Also try to get an existing scope
        await scpMgr.GetScope("api1.read");
        
        // Try to get a non-existent scope
        await scpMgr.GetScope("non-existent-scope");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Test08 Error: {ex.Message}");
    }
    Console.WriteLine();
}

// Test 09: Update scope
async Task Test09_UpdateScope()
{
    Console.WriteLine("=== Test09: Update Scope ===");
    try
    {
        var (_, scpMgr) = CreateManagers();
        
        // Update the test scope we created
        await scpMgr.UpdateScope(
            "test-scope-01",
            "Updated Test Scope 01",
            new[] { "updated-api", "updated-resource" }
        );
        
        // Try to update a non-existent scope
        await scpMgr.UpdateScope("non-existent-scope", "Won't work", null);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Test09 Error: {ex.Message}");
    }
    Console.WriteLine();
}

// Test 10: Delete scope
async Task Test10_DeleteScope()
{
    Console.WriteLine("=== Test10: Delete Scope ===");
    try
    {
        var (_, scpMgr) = CreateManagers();
        
        // Delete the test scope we created
        await scpMgr.DeleteScope("test-scope-01");
        
        // Try to delete a non-existent scope
        await scpMgr.DeleteScope("non-existent-scope");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Test10 Error: {ex.Message}");
    }
    Console.WriteLine();
}

// Helper method to create managers (using CLI Program.cs)
(ApplicationManagement, ScopeManagement) CreateManagers()
{
    SimpleIdentityServer.CLI.Program.CreateManagers(out var appMgr, out var scpMgr);
    return (appMgr, scpMgr);
}


