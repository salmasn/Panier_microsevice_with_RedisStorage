using PanierService.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configuration du port Railway
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    Console.WriteLine($"🚀 Écoute sur le port: {port}");
    options.ListenAnyIP(int.Parse(port));
});

// ✅ Configuration Redis - PARSER L'URL CORRECTEMENT
var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL")
    ?? Environment.GetEnvironmentVariable("REDIS_PRIVATE_URL")
    ?? "redis://localhost:6379";

Console.WriteLine($"🔴 Redis URL brute: {redisUrl}");

string redisConnectionString;

try
{
    if (redisUrl.StartsWith("redis://"))
    {
        // Parser l'URL Redis
        var uri = new Uri(redisUrl);
        var host = uri.Host;
        var port = uri.Port;
        var password = string.Empty;

        if (!string.IsNullOrEmpty(uri.UserInfo) && uri.UserInfo.Contains(':'))
        {
            password = uri.UserInfo.Split(':')[1];
        }

        // ✅ Format attendu par StackExchange.Redis avec abortConnect=false
        redisConnectionString = $"{host}:{port},password={password},ssl=false,abortConnect=false,connectTimeout=15000,syncTimeout=5000";

        Console.WriteLine($"✅ Redis parsé: {host}:{port}");
    }
    else
    {
        // Format déjà correct
        redisConnectionString = redisUrl + ",abortConnect=false";
    }

    // Connexion à Redis
    var redis = ConnectionMultiplexer.Connect(redisConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

    Console.WriteLine("✅ Connexion Redis réussie");

    // Test de connexion
    var db = redis.GetDatabase();
    db.StringSet("test", "ok", TimeSpan.FromSeconds(10));
    Console.WriteLine("✅ Test Redis OK");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erreur Redis: {ex.Message}");
    Console.WriteLine($"   Type: {ex.GetType().Name}");
    Console.WriteLine($"   Stack: {ex.StackTrace}");

    // ⚠️ Ne pas throw pour permettre à l'app de démarrer
    // On peut utiliser un fallback en mémoire si besoin
}

builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<IPanierService, PanierServiceImpl>();

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

app.UseCors("AllowAll");

// Swagger en production pour tester
app.UseSwagger();
app.UseSwaggerUI();

// ❌ PAS de HTTPS redirect sur Railway
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        redis = "connected"
    });
});

Console.WriteLine("🚀 Application démarrée");
app.Run();