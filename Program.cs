using portfolio_backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure port for Heroku
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://+:{port}");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Swagger without OpenApi
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Get connection string from environment variable or configuration
string connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ?? 
    builder.Configuration.GetConnectionString("DefaultConnection");

// Register the DatabaseService as a singleton
builder.Services.AddSingleton(new DatabaseService(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Comment out HTTPS redirection for Heroku
// app.UseHttpsRedirection();

// Use CORS before routing
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();