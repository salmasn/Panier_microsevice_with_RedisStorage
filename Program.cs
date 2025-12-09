using PanierService.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configuration du port Railway
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    Console.WriteLine($"Écoute sur le port: {port}");
    options.ListenAnyIP(int.Parse(port));
});

// ✅ Configuration Redis
var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL")
    ?? Environment.GetEnvironmentVariable("REDIS_PRIVATE_URL")
    ?? "redis://localhost:6379";

Console.WriteLine($"Redis URL brute: {redisUrl}");

string redisConnectionString;
try
{
    if (redisUrl.StartsWith("redis://"))
    {
        var uri = new Uri(redisUrl);
        var host = uri.Host;
        var port = uri.Port;
        var password = string.Empty;

        if (!string.IsNullOrEmpty(uri.UserInfo) && uri.UserInfo.Contains(':'))
        {
            password = uri.UserInfo.Split(':')[1];
        }

        redisConnectionString = $"{host}:{port},password={password},ssl=false,abortConnect=false,connectTimeout=15000,syncTimeout=5000";
        Console.WriteLine($"Redis parsé: {host}:{port}");
    }
    else
    {
        redisConnectionString = redisUrl + ",abortConnect=false";
    }

    var redis = ConnectionMultiplexer.Connect(redisConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    Console.WriteLine("Connexion Redis réussie");

    var db = redis.GetDatabase();
    db.StringSet("test", "ok", TimeSpan.FromSeconds(10));
    Console.WriteLine("Test Redis OK");
}
catch (Exception ex)
{
    Console.WriteLine($"Erreur Redis: {ex.Message}");
}

builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<IPanierService, PanierServiceImpl>();

// ✅ CORS - AUTORISER LE FRONT-END LOCAL
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5242",
                "https://localhost:7231"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ ACTIVER CORS
app.UseCors("AllowFrontend");

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        redis = "connected"
    });
});

Console.WriteLine("Application démarrée");
app.Run();