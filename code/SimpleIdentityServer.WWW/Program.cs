using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SimpleIdentityServer.WWW.Areas.Identity;
using SimpleIdentityServer.WWW.Data;
using EmailProviders.MailHogEmailProvider;
using EmailProviders.SendGridEmailProvider;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Configure Entity Framework Core to use Microsoft SQL Server.
    options.UseSqlServer(connectionString);

    // Register the entity sets needed by OpenIddict.
    // Note: use the generic overload if you need to replace the default OpenIddict entities.
    options.UseOpenIddict();
});

builder.Services.AddOpenIddict()
    // Register the OpenIddict core components.
    .AddCore(options =>
    {
        // Configure OpenIddict to use the Entity Framework Core stores and models.
        // Note: call ReplaceDefaultEntities() to replace the default entities.
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    // Register the OpenIddict server components.
    .AddServer(options =>
    {
        // Enable the token endpoint.
        options.SetTokenEndpointUris("connect/token");

        // Enable the client credentials flow.
        options.AllowClientCredentialsFlow();

        // Register the signing and encryption credentials.
        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core options.
        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough();
    });

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(config =>
{
    config.SignIn.RequireConfirmedEmail = true;
    config.Tokens.ProviderMap.Add("CustomEmailConfirmation",
        new TokenProviderDescriptor(
            typeof(CustomEmailConfirmationTokenProvider<IdentityUser>)));
    config.Tokens.EmailConfirmationTokenProvider = "CustomEmailConfirmation";
}).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddTransient<CustomEmailConfirmationTokenProvider<IdentityUser>>();

builder.Services.AddControllersWithViews();

// Add Serilog
var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder().AddConfiguration(builder.Configuration).Build())
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

// Retrieve environment variables
const string mailProviderMailHog = "MailHog";
const string mailProviderSendGrid = "SendGrid";

var emailProvider = builder.Configuration.GetSection("EmailProvider").Value
                     ?? throw new InvalidOperationException("Configuration variable 'EmailProvider' not found.");

switch (emailProvider)
{
    case mailProviderSendGrid:
        var sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY")
                             ?? throw new ArgumentException("Environment variable 'SENDGRID_API_KEY' not found.");
        var fromEmail = Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL")
                        ?? throw new ArgumentException("Environment variable 'SENDGRID_FROM_EMAIL' not found.");
        var fromName = Environment.GetEnvironmentVariable("SENDGRID_FROM_NAME")
                       ?? throw new ArgumentException("Environment variable 'SENDGRID_FROM_NAME' not found.");

        // Register SendGridEmailSender with DI
        builder.Services.AddTransient<IEmailSender>(provider =>
            new SendGridEmailSender(logger, sendGridApiKey, fromEmail, fromName));

        break;
    case mailProviderMailHog:
        var mailHogEmail = builder.Configuration.GetSection("MailHog").GetSection("FromEmail").Value
                           ?? throw new ArgumentException("Configuration variable 'MailHog.FromEmail' not found.");
        var mailHogServer = builder.Configuration.GetSection("MailHog").GetSection("Host").Value
                            ?? throw new ArgumentException("Configuration variable 'MailHog.Host' not found.");
        var mailHogPort = builder.Configuration.GetSection("MailHog").GetSection("Port").Value
                          ?? throw new ArgumentException("Configuration variable 'MailHog.Port' not found.");

        var mailHogPortParsingSucceeded = int.TryParse(mailHogPort, out int mailHogPortValue);
        if (!mailHogPortParsingSucceeded)
        {
            throw new ArgumentException("Configuration variable 'MailHog.Port' is not a valid integer.");
        }
        
        // Register MailHogEmailSender with DI
        builder.Services.AddTransient<IEmailSender>(provider =>
            new MailHogEmailSender(logger, mailHogEmail, mailHogServer, mailHogPortValue));

        break;
    default:
        throw new ArgumentException($"Invalid email provider '{emailProvider}'. Supported providers are: {mailProviderSendGrid}, {mailProviderMailHog}");
}

//set the email inactivity timeout to 5 days
builder.Services.ConfigureApplicationCookie(o => {
    o.ExpireTimeSpan = TimeSpan.FromDays(5);
    o.SlidingExpiration = true;
});

// set all data protection tokens timeout period to 3 hours
builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
    o.TokenLifespan = TimeSpan.FromHours(3));

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseForwardedHeaders();

app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

//app.UseEndpoints(options =>
//{
//    options.MapControllers();
//    options.MapDefaultControllerRoute();
//});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
