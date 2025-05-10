FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the files and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out .

# Add code to apply migrations on startup
# This is handled in Program.cs with the Database.Migrate() call

# Heroku uses the PORT environment variable
CMD ASPNETCORE_URLS=http://*:$PORT dotnet PORTFOLIO-BACKEND.dll