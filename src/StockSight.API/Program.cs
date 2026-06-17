using Hangfire;
using Hangfire.PostgreSql;
using StockSight.API.BackgroundServices;
using StockSight.API.Hubs;
using StockSight.Core.Interfaces;
using StockSight.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---- MVC + OpenAPI ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---- SignalR ----
builder.Services.AddSignalR();
builder.Services.AddSingleton<IStockBroadcaster, SignalRStockBroadcaster>();
builder.Services.Configure<StockIngestionOptions>(builder.Configuration.GetSection(StockIngestionOptions.SectionName));
builder.Services.AddHostedService<StockDataIngestionService>();

// ---- Infrastructure: PostgreSQL (EF Core), Redis, market data ----
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Hangfire (PostgreSQL storage) ----
var hangfireConn = builder.Configuration.GetConnectionString("Postgres");
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(hangfireConn)));
builder.Services.AddHangfireServer();

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

app.MapControllers();
app.MapHub<StockHub>("/hubs/stocks");
app.UseHangfireDashboard("/hangfire");

app.Run();
