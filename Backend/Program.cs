using Backend.Common.Security;
using Backend.Common.Email;
using Backend.Common.Seed;
using Backend.Common.ChirpStack;
using Backend.Common.Sync;
using Backend.Common;
using Backend.Features.Auth;
using Backend.Features.Organisations;
using Backend.Features.Users;
using Backend.Features.Gateways;
using Backend.Features.Nodes;
using Backend.Features.DeviceProfiles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;


var builder = WebApplication.CreateBuilder(args);


// --- Config ---
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

// --- Database ---
var connectionString = builder.Configuration.GetConnectionString("PostgresDb");
builder.Services.AddScoped(_ => new NpgsqlConnection(connectionString));

// --- Super Admin Seed ---
builder.Services.Configure<SuperAdminSeedSettings>(builder.Configuration.GetSection("Seed:SuperAdmin"));
builder.Services.AddScoped<DbSeeder>();

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

// --- Gateway Service ---
builder.Services.Configure<ChirpStackSettings>(builder.Configuration.GetSection("ChirpStack"));
builder.Services.AddSingleton<IChirpStackClient, ChirpStackClient>();
builder.Services.AddScoped<IGatewayService, GatewayService>();

// --- Node Service ---
builder.Services.Configure<CryptoSettings>(builder.Configuration.GetSection("Crypto"));
builder.Services.AddSingleton<IAppKeyCipher, AppKeyCipher>();
builder.Services.AddScoped<INodeService, NodeService>();

// --- Device profile Service ---
builder.Services.AddScoped<IDeviceProfileService, DeviceProfileService>();

// --- Worker Service ---
builder.Services.AddHostedService<ChirpStackSyncWorker>();

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
app.MapGatewayEndpoints();
app.MapNodeEndpoints();
app.MapDeviceProfileEndpoints();

app.Run();