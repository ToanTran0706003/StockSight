using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using System.Text;
using StockSight.API.BackgroundServices;
using StockSight.API.Hubs;
using StockSight.Core.Interfaces;
using StockSight.Infrastructure;
using StockSight.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// ---- MVC + OpenAPI ----
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---- Auth ----
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "stocksight-local-dev-secret-key-change-me";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "StockSight";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "StockSight.Web";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// ---- SignalR ----
builder.Services.AddSignalR();
builder.Services.AddSingleton<IStockBroadcaster, SignalRStockBroadcaster>();
builder.Services.Configure<StockIngestionOptions>(builder.Configuration.GetSection(StockIngestionOptions.SectionName));
builder.Services.AddHostedService<StockDataIngestionService>();
builder.Services.AddHostedService<AlertCheckerService>();

// ---- Infrastructure: PostgreSQL (EF Core), Redis, market data ----
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Hangfire (PostgreSQL storage) ----
var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", true);
if (hangfireEnabled)
{
    var hangfireConn = builder.Configuration.GetConnectionString("Postgres");
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(hangfireConn)));
    builder.Services.AddHangfireServer();
}

// ---- CORS for the Blazor WASM client ----
const string CorsPolicy = "BlazorClient";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "https://localhost:7000", "http://localhost:5000" })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<StockHub>("/hubs/stocks");
if (hangfireEnabled)
    app.UseHangfireDashboard("/hangfire");

if (builder.Configuration.GetValue("Data:UseInMemory", false))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<StockSightDbContext>().Database.EnsureCreated();
}

app.Run();
