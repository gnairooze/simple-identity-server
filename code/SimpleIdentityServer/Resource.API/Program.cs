using Microsoft.AspNetCore.Authentication;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Validation.ServerIntegration;
using OpenIddict.Validation.SystemNetHttp;
using Resource.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure authentication to use OpenIddict validation
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

// Configure OpenIddict Validation with introspection (required for encrypted tokens)
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer(builder.Configuration["IdentityServer:Authority"]!);
        options.AddAudiences(builder.Configuration["IdentityServer:Audience"]!);
        
        // Configure the validation handler to use introspection for encrypted tokens
        options.UseIntrospection()
               .SetClientId(builder.Configuration["IdentityServer:ClientId"]!)
               .SetClientSecret(builder.Configuration["IdentityServer:ClientSecret"]!);
        
        // Configure the validation handler to use ASP.NET Core.
        options.UseAspNetCore();
        
        // Configure the validation handler to use System.Net.Http for introspection.
        options.UseSystemNetHttp();
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireApi1Read", policy =>
        policy.RequireClaim("scope", "api1.read"));
    
    options.AddPolicy("RequireApi1Write", policy =>
        policy.RequireClaim("scope", "api1.write"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Add response filtering middleware for field-level authorization
app.UseMiddleware<ResponseFilteringMiddleware>();

app.MapControllers();

app.Run();
