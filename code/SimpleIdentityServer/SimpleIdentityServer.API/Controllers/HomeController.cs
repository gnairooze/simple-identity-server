using Microsoft.AspNetCore.Mvc;

namespace SimpleIdentityServer.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public IActionResult Index()
    {
        return Ok(new
        {
            Message = "Simple Identity Server",
            Version = "1.0.0",
            Description = "OAuth2/OpenID Connect Identity Server using OpenIddict",
            Endpoints = new
            {
                Token = "/connect/token",
                Introspection = "/connect/introspect",
                Configuration = "/.well-known/openid-configuration",
                Jwks = "/.well-known/jwks"
            },
            SupportedGrantTypes = new[] { "client_credentials" },
            Documentation = "See /swagger for API documentation"
        });
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
} 