using Backend.Common.Security;
using Backend.Common.Email;
using Backend.Common.Seed;
using Backend.Common;
using Backend.Features.Auth;
using Backend.Features.Organisations;
using Backend.Features.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// --- Config ---
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwt = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

// --- Super Admin Seed ---
builder.Services.Configure<SuperAdminSeedSettings>(builder.Configuration.GetSection("Seed:SuperAdmin"));
builder.Services.AddScoped<DbSeeder>();

// --- Database ---
var connectionString = builder.Configuration.GetConnectionString("PostgresDb");
builder.Services.AddScoped(_ => new NpgsqlConnection(connectionString));

// --- Security ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// --- Organisation Service ---
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));
builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();
builder.Services.AddScoped<IOrganisationService, OrganisationService>();

// --- User Service ---
builder.Services.AddScoped<IUserService, UserService>();

// --- Jwt Authentication ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.SuperAdminOnly, p => p.RequireRole(Roles.SuperAdmin));
    options.AddPolicy(Policies.OrgAdminOrAbove, p => p.RequireRole(Roles.SuperAdmin, Roles.OrgAdmin));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedSuperAdminAsync();
}

// Pipeline order

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapOrganisationsEndpoints();
app.MapUserEndpoints();

app.Run();