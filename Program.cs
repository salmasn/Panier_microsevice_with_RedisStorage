using PanierService.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configuration du port pour Railway
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    options.ListenAnyIP(int.Parse(port));
});

// Redis Configuration
// Redis Configuration avec Railway
var redisConnection = Environment.GetEnvironmentVariable("REDIS_URL")
    ?? builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";

Console.WriteLine($"🔴 Connexion Redis : {redisConnection}");

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<IPanierService, PanierServiceImpl>();

// ✅ Configuration CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ ACTIVER CORS
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ⚠️ IMPORTANT : Ne pas utiliser HTTPS redirect sur Railway
// app.UseHttpsRedirection(); // ← COMMENTE CETTE LIGNE

app.UseAuthorization();
app.MapControllers();
app.Run();