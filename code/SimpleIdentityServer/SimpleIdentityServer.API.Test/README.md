# SimpleIdentityServer.API.Test

Comprehensive test suite for the SimpleIdentityServer API, focusing on the `/connect/token` and `/connect/introspect` endpoints.

## Overview

This test project provides thorough coverage of the OAuth2 Client Credentials flow implementation using OpenIddict. It includes unit tests, integration tests, security tests, and performance tests.

## Test Categories

### 1. Token Endpoint Tests (`TokenControllerTests.cs`)
- **Valid Requests**: Tests successful token generation with various client configurations
- **Invalid Credentials**: Tests rejection of invalid client IDs and secrets
- **Grant Type Validation**: Tests support for client_credentials flow and rejection of unsupported flows
- **Scope Handling**: Tests proper scope validation and assignment
- **Error Scenarios**: Tests various error conditions and proper error responses
- **Security**: Tests input validation and malformed request handling

### 2. Introspection Endpoint Tests (`IntrospectControllerTests.cs`)
- **Valid Token Introspection**: Tests successful introspection of active tokens
- **Field-Level Authorization**: Tests different levels of information disclosure based on client permissions
- **Invalid Token Handling**: Tests proper response for invalid, expired, or malformed tokens
- **Client Authentication**: Tests client credential validation for introspection requests
- **Cross-Client Introspection**: Tests introspection of tokens issued to different clients
- **Security**: Tests protection against token enumeration and unauthorized access

### 3. Integration Tests (`TokenLifecycleTests.cs`)
- **Token Lifecycle**: Tests complete flow from token creation to introspection
- **Multi-Client Scenarios**: Tests interactions between different client types
- **Consistency Validation**: Tests data consistency across token and introspection operations
- **Timing Validation**: Tests token expiration and creation timestamps

### 4. Security Tests (`SecurityTests.cs`)
- **SQL Injection Protection**: Tests resistance to SQL injection attacks
- **XSS Protection**: Tests input sanitization against XSS attempts
- **Input Validation**: Tests handling of malformed data, null bytes, and invalid characters
- **Security Headers**: Validates presence of appropriate security headers
- **Payload Size Limits**: Tests handling of oversized requests

### 5. Performance Tests (`LoadTests.cs`)
- **Concurrent Load**: Tests system behavior under concurrent request load
- **Sequential Performance**: Tests performance consistency over multiple requests
- **Mixed Workload**: Tests performance with mixed token and introspection requests
- **Response Time Validation**: Ensures acceptable response times under various conditions

## Test Infrastructure

### TestWebApplicationFactory
- Sets up in-memory database for isolated testing
- Configures test-specific settings and logging
- Seeds test data (clients, scopes, applications)
- Provides clean test environment for each test run

### TestBase
- Base class for all test classes providing common functionality
- HTTP client management and request helpers
- Response deserialization utilities
- Token generation helpers for test scenarios

### Test Data
The test suite includes pre-configured test clients:

- **service-api**: Full permissions (read/write scopes)
- **web-app**: Limited permissions (read scope only)
- **mobile-app**: Full permissions (read/write scopes)
- **admin-client**: Administrative permissions for introspection
- **invalid-client**: Client without proper permissions (for negative testing)

## Running the Tests

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension

### Command Line
```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "Category=Integration"

# Run with detailed output
dotnet test --verbosity detailed

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Visual Studio
- Open the solution in Visual Studio
- Build the solution (Ctrl+Shift+B)
- Open Test Explorer (Test → Test Explorer)
- Run all tests or select specific tests

### VS Code
- Open the project in VS Code
- Install the C# extension if not already installed
- Use the Testing panel to run tests
- Or use the integrated terminal with dotnet test commands

## Test Configuration

### appsettings.Test.json
- Configures in-memory database for testing
- Sets appropriate logging levels for test environment
- Configures rate limiting with higher thresholds for testing
- Disables security features that might interfere with testing

### Environment Variables
Tests run in the "Test" environment and use test-specific configurations.

## Coverage Areas

### Functional Coverage
- ✅ OAuth2 Client Credentials flow
- ✅ Token generation and validation
- ✅ Token introspection
- ✅ Scope-based authorization
- ✅ Client authentication
- ✅ Error handling and responses

### Security Coverage
- ✅ Input validation and sanitization
- ✅ SQL injection protection
- ✅ XSS protection
- ✅ Authentication bypass attempts
- ✅ Token enumeration protection
- ✅ Information disclosure prevention

### Performance Coverage
- ✅ Concurrent request handling
- ✅ Response time validation
- ✅ Memory usage (via in-memory database)
- ✅ Scalability under load
- ✅ Resource cleanup

### Edge Cases
- ✅ Malformed requests
- ✅ Invalid content types
- ✅ Oversized payloads
- ✅ Special characters and encoding
- ✅ Network timeout scenarios
- ✅ Database connection issues (simulated)

## Best Practices Implemented

1. **Test Isolation**: Each test runs in isolation with clean database state
2. **Parallel Execution**: Tests are designed to run in parallel safely
3. **Comprehensive Assertions**: Multiple assertions per test to validate complete behavior
4. **Error Message Validation**: Tests validate both status codes and error messages
5. **Performance Benchmarks**: Establishes baseline performance expectations
6. **Security-First Testing**: Proactive testing of security vulnerabilities
7. **Real-World Scenarios**: Tests simulate actual usage patterns

## Continuous Integration

The test suite is designed to run in CI/CD pipelines:
- No external dependencies (uses in-memory database)
- Deterministic results
- Appropriate timeouts for CI environments
- Detailed logging for debugging failures
- Code coverage reporting

## Extending the Tests

To add new tests:

1. **New Test Class**: Inherit from `TestBase` for infrastructure support
2. **Test Categories**: Use appropriate namespaces (`Controllers`, `Integration`, `Security`, `Performance`)
3. **Test Naming**: Use descriptive names following the pattern: `Method_Scenario_ExpectedBehavior`
4. **Assertions**: Use FluentAssertions for readable and comprehensive assertions
5. **Cleanup**: Ensure proper resource cleanup in tests

## Troubleshooting

### Common Issues

1. **Test Database Issues**: Tests use in-memory database that's recreated for each test
2. **Port Conflicts**: TestWebApplicationFactory handles port allocation automatically
3. **Timing Issues**: Tests include appropriate waits and timeouts
4. **Parallel Execution**: Tests are designed to be thread-safe

### Debugging Tests
- Set breakpoints in test methods
- Use detailed logging output (`--verbosity detailed`)
- Check test output for performance metrics and error details
- Use Visual Studio Test Explorer for detailed test results

## Contributing

When adding new tests:
1. Follow existing patterns and naming conventions
2. Include both positive and negative test cases
3. Add appropriate security and performance considerations
4. Update this README if adding new test categories
5. Ensure tests are deterministic and can run in parallel
