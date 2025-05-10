using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FeedbackApi.Data;
using Microsoft.OpenApi.Models;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add environment variables support
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();

// Add CORS support
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", 
                          "https://seanththomas.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure for Heroku - get port from environment variable
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
    Console.WriteLine($"Configured to listen on PORT: {port}");
}

// Handle JawsDB URL for Heroku
var jawsDbUrl = Environment.GetEnvironmentVariable("JAWSDB_URL");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(jawsDbUrl))
{
    Console.WriteLine("Found JAWSDB_URL environment variable");
    try 
    {
        var uri = new Uri(jawsDbUrl);
        var userInfo = uri.UserInfo.Split(':');
        var server = uri.Host;
        var database = uri.AbsolutePath.TrimStart('/');
        var user = userInfo[0];
        var password = userInfo[1];
        var dbPort = uri.Port > 0 ? uri.Port : 3306;
        
        connectionString = $"Server={server};Port={dbPort};Database={database};User={user};Password={password};";
        Console.WriteLine($"Parsed connection string from JAWSDB_URL: Server={server};Database={database};User={user};Password=********");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error parsing JAWSDB_URL: {ex.Message}");
    }
}
else if (!string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("Using connection string from configuration");
}
else
{
    Console.WriteLine("WARNING: No database connection string found!");
}

// Configure MySQL - Fixed version
if (!string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine($"Configuring database with connection string: {connectionString?.Split(';')[0]}");
    
    // Use a specific MySQL server version instead of auto-detect
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));
    
    try
    {
        builder.Services.AddDbContext<FeedbackDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));
        Console.WriteLine("Database context configured successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error configuring database context: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}
else
{
    Console.WriteLine("ERROR: No connection string available, database will not be configured");
    // Add a dummy DbContext to prevent DI errors
    builder.Services.AddDbContext<FeedbackDbContext>(options =>
        options.UseInMemoryDatabase("TestDb"));
}

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Feedback API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Feedback API v1"));
}
else
{
    // In production, handle exceptions differently
    app.UseExceptionHandler("/Error");
}

// Add diagnostic endpoints
app.MapGet("/", () => new
{
    status = "UP",
    timestamp = DateTime.UtcNow,
    message = "Feedback API is running",
    connectionString = !string.IsNullOrEmpty(connectionString) ? "Configured" : "Not configured"
});

app.MapGet("/health", () => new
{
    status = "UP",
    timestamp = DateTime.UtcNow,
    databaseConnectionString = !string.IsNullOrEmpty(connectionString) ? 
        $"Configured (length: {connectionString.Length})" : "Not configured",
    environmentVariables = new 
    {
        port = Environment.GetEnvironmentVariable("PORT") ?? "Not set",
        jawsDbUrl = Environment.GetEnvironmentVariable("JAWSDB_URL") != null ? 
            $"Set (length: {Environment.GetEnvironmentVariable("JAWSDB_URL").Length})" : "Not set",
        aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not set"
    }
});

// Test database after app startup, with better error handling
if (!string.IsNullOrEmpty(connectionString))
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            Console.WriteLine("Attempting to resolve DbContext...");
            var dbContext = scope.ServiceProvider.GetRequiredService<FeedbackDbContext>();
            
            Console.WriteLine("Testing database connection...");
            var canConnect = dbContext.Database.CanConnect();
            Console.WriteLine($"Database connection test: {(canConnect ? "Successful" : "Failed")}");
            
            if (canConnect)
            {
                Console.WriteLine("Creating database if it doesn't exist...");
                dbContext.Database.EnsureCreated();
                Console.WriteLine("Database created/verified successfully");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR during database initialization: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

// Use CORS
app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("Application configured successfully, starting...");

app.Run();