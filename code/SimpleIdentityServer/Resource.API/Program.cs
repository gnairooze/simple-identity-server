using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Validation.ServerIntegration;
using OpenIddict.Validation.SystemNetHttp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["IdentityServer:Authority"];
        options.Audience = builder.Configuration["IdentityServer:Audience"];
        options.RequireHttpsMetadata = false; // Set to true in production
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["IdentityServer:Authority"],
            ValidAudience = builder.Configuration["IdentityServer:Audience"]
        };
    });

// Configure OpenIddict Validation (for introspection)
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        options.SetIssuer(builder.Configuration["IdentityServer:Authority"]!);
        options.AddAudiences(builder.Configuration["IdentityServer:Audience"]!);
        
        // Configure the validation handler to use introspection
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

app.MapControllers();

app.Run();
