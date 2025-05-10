using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FeedbackApi.Data;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add environment variables support
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
            "https://seanhthomas.com",
            "https://www.seanhthomas.com",
            "http://localhost:3000")
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

// Handle database connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// If no connection string from config, try environment variable directly
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("Using connection string from environment variable");
    }
}

// Also check for JAWSDB_URL
var jawsDbUrl = Environment.GetEnvironmentVariable("JAWSDB_URL");
if (string.IsNullOrEmpty(connectionString) && !string.IsNullOrEmpty(jawsDbUrl))
{
    Console.WriteLine("Found JAWSDB_URL, parsing connection string");
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
        Console.WriteLine($"Parsed connection string from JAWSDB_URL");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error parsing JAWSDB_URL: {ex.Message}");
    }
}

Console.WriteLine($"Connection string status: {(!string.IsNullOrEmpty(connectionString) ? "Found" : "Not found")}");

// Configure MySQL with the connection string
if (!string.IsNullOrEmpty(connectionString))
{
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));

    builder.Services.AddDbContext<FeedbackDbContext>(options =>
        options.UseMySql(connectionString, serverVersion));

    Console.WriteLine("Database context configured");
}
else
{
    Console.WriteLine("ERROR: No connection string found!");
    // Use in-memory database as fallback
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

app.UseCors("AllowReactApp");

// Enable Swagger in all environments (updated section)
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Feedback API v1");
    c.RoutePrefix = "swagger"; // Swagger UI will be available at /swagger
});

// Configure exception handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// Update the root endpoint to provide links
app.MapGet("/", () => new
{
    status = "UP",
    timestamp = DateTime.UtcNow,
    message = "Feedback API is running",
    connectionString = !string.IsNullOrEmpty(connectionString) ? "Configured" : "Not configured",
    links = new
    {
        swagger = "/swagger",
        health = "/health",
        testDb = "/test-db",
        feedback = "/api/feedback"
    }
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
        connectionStringsDefaultConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") != null ?
            $"Set (length: {Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection").Length})" : "Not set",
        aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not set"
    }
});

// Test database endpoint
app.MapGet("/test-db", async () =>
{
    var result = new Dictionary<string, object>();

    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FeedbackDbContext>();

        // Test basic connection
        var canConnect = await dbContext.Database.CanConnectAsync();
        result["canConnect"] = canConnect;

        // Get actual connection string being used (masked)
        var activeConnStr = dbContext.Database.GetConnectionString();
        if (activeConnStr != null)
        {
            var parts = activeConnStr.Split(';');
            result["server"] = parts.FirstOrDefault(p => p.StartsWith("Server="))?.Replace("Server=", "") ?? "Unknown";
        }

        // Check if tables exist
        if (canConnect)
        {
            try
            {
                var feedbackCount = await dbContext.Feedbacks.CountAsync();
                result["feedbackCount"] = feedbackCount;
                result["feedbacksTableExists"] = true;
            }
            catch (Exception tableEx)
            {
                result["feedbacksTableExists"] = false;
                result["tableError"] = tableEx.Message;

                // Try to create the table
                try
                {
                    await dbContext.Database.EnsureCreatedAsync();
                    result["tableCreationAttempt"] = "Success";
                }
                catch (Exception createEx)
                {
                    result["tableCreationAttempt"] = "Failed";
                    result["tableCreationError"] = createEx.Message;
                }
            }
        }
    }
    catch (Exception ex)
    {
        result["error"] = ex.Message;
        result["type"] = ex.GetType().Name;
        if (ex.InnerException != null)
        {
            result["innerError"] = ex.InnerException.Message;
            result["innerType"] = ex.InnerException.GetType().Name;
        }
    }

    result["timestamp"] = DateTime.UtcNow;
    return result;
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