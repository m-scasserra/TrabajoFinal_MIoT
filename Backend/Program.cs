using Backend.Common.Security;
using Backend.Features.Auth;
using Npgsql;


var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PostgresDb");
builder.Services.AddScoped(_ => new NpgsqlConnection(connectionString));

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

app.MapAuthEndpoints();

app.Run();