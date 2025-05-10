using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FeedbackApi.Data;
using Microsoft.OpenApi.Models;
using System;
using System.Text;

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
        var port = uri.Port > 0 ? uri.Port : 3306;
        
        connectionString = $"Server={server};Port={port};Database={database};User={user};Password={password};";
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
    connectionString = "Server=localhost;Database=feedbackdb;User=root;Password=password;"; // Fallback for dev
}

// Configure MySQL
try
{
    builder.Services.AddDbContext<FeedbackDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
    Console.WriteLine("Database context configured successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Error configuring database context: {ex.Message}");
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
    // In production, you might want to handle exceptions differently
    app.UseExceptionHandler("/Error");
}

// Add a diagnostic endpoint at the root
app.MapGet("/", () => new
{
    status = "UP",
    timestamp = DateTime.UtcNow,
    message = "Feedback API is running"
});

// Add health endpoint
app.MapGet("/health", () => new
{
    status = "UP",
    timestamp = DateTime.UtcNow,
    databaseConnectionString = !string.IsNullOrEmpty(connectionString) ? "Configured" : "Not configured",
    environmentVariables = new 
    {
        port = Environment.GetEnvironmentVariable("PORT") ?? "Not set",
        jawsDbUrl = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JAWSDB_URL")) ? "Set" : "Not set",
        aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not set"
    }
});

// Ensure database is created and migrations are applied on startup - with robust error handling
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
        else
        {
            Console.WriteLine("WARNING: Could not connect to database");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR during database initialization: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    // Continue app startup despite database errors
}

// For Heroku, disable HTTPS redirection
app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("Application configured successfully, starting...");

app.Run();