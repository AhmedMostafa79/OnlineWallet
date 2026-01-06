using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OnlineWallet.Application.Helpers;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Application.Interfaces.TokenStrategies;
using OnlineWallet.Application.Models;
using OnlineWallet.Application.Services;
using OnlineWallet.Application.Services.TokenStrategies;
using OnlineWallet.Infrastructure;
using OnlineWallet.Infrastructure.Caching.Decorators;
using OnlineWallet.Infrastructure.Caching.Services;
using OnlineWallet.Infrastructure.Interfaces;
using OnlineWallet.Infrastructure.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);


    // Add JWT Authentication to swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme. 
                      Enter your token in the text input below (without 'Bearer' prefix).
                      Example: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer" 
            },
            Scheme = "Bearer",
            Name = "Authorization",
            In = ParameterLocation.Header
        },
        new List<string>()  // No OAuth2 scopes needed for JWT
    }
});
});

builder.Services.Configure<JWT>(builder.Configuration.GetSection("JWT"));

// Add Redis Caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    // Read the connection string from configuration (e.g., appsettings.json)
    options.Configuration = builder.Configuration.GetConnectionString("Redis");


});
// Add health checks
builder.Services.AddHealthChecks();


    // Registering Cache Key Services
builder.Services.AddScoped<UsersCacheKeyService>();
builder.Services.AddScoped<AccountsCacheKeyService>();
builder.Services.AddScoped<AuditLogsCacheKeyService>();

// Dependency Injection

//Register IRepositories with their cached decorators
// For Users Repository
builder.Services.AddScoped<UsersRepository>();
builder.Services.AddScoped<IUsersRepository>(serviceProvider =>
{
    var innerRepo = serviceProvider.GetRequiredService<UsersRepository>();
    var cache = serviceProvider.GetRequiredService<IDistributedCache>();
    var keyService = serviceProvider.GetRequiredService<UsersCacheKeyService>();
    return new CachedUsersRepository(innerRepo, cache, keyService);

});

//Database seeding 
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<PasswordHasher<object>>();

// For Accounts Repository
builder.Services.AddScoped<AccountsRepository>();
builder.Services.AddScoped<IAccountsRepository>(serviceProvider =>
{
    var innerRepo = serviceProvider.GetRequiredService<AccountsRepository>();
    var cache = serviceProvider.GetRequiredService<IDistributedCache>();
    var keyService = serviceProvider.GetRequiredService<AccountsCacheKeyService>();
    return new CachedAccountsRepository(innerRepo, cache, keyService);
});
// For AuditLogs Repository
builder.Services.AddScoped<AuditLogsRepository>();
builder.Services.AddScoped<IAuditLogsRepository>(serviceProvider =>
{
    var innerRepo = serviceProvider.GetRequiredService<AuditLogsRepository>();
    var cache = serviceProvider.GetRequiredService<IDistributedCache>();
    var keyService = serviceProvider.GetRequiredService<AuditLogsCacheKeyService>();
    return new CachedAuditLogsRepository(innerRepo, cache, keyService);
});

//Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


    //Services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IAuthService, AuthService>();

    //Tokens
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();


// Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting"));

builder.Services.Configure<IpRateLimitPolicies>(
    builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();

builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>(); 

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false;
        o.SaveToken = false;
        o.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])),
            RoleClaimType=ClaimTypes.Role
        };
    });

// Database and EF configuration
builder.Services.AddDbContext<WalletDbContext>(options=>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlOptions=>sqlOptions.MigrationsAssembly("OnlineWallet.Infrastructure")));

//System building
var app = builder.Build();

// Apply migrations automatically on startup (for development)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
    dbContext.Database.Migrate();

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedManagerAsync();
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OnlineWallet v1");
});

// Add Health Checks
app.MapHealthChecks("/health");

// Comment out HTTPS redirection in Docker/containerized environments
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseIpRateLimiting();
app.MapControllers();

await app.RunAsync();

