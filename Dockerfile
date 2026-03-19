# --- STAGE 1: BUILD ---
# Use the .NET SDK image to compile the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies (Optimizes layer caching)
COPY ["CarInsuranceBot.csproj", "./"]
RUN dotnet restore "./CarInsuranceBot.csproj"

# Copy the entire source code and publish the release build
COPY . .
RUN dotnet publish "CarInsuranceBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- STAGE 2: RUNTIME ---
# Use a lightweight ASP.NET runtime image for production
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy the compiled output from the build stage
COPY --from=build /app/publish .

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "CarInsuranceBot.dll"]