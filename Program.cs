using portfolio_backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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

// Get connection string from environment variable or use the provided one
string connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ?? 
    "mysql://blagqvfcqso6b1qv:ya3q0nltkzznr2xg@k2pdcy98kpcsweia.cbetxkdyhwsb.us-east-1.rds.amazonaws.com:3306/a95jbn9sw86796nl";

// Register the DatabaseService as a singleton
builder.Services.AddSingleton(new DatabaseService(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS before routing
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();