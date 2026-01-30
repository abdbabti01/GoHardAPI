FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY GoHardAPI/GoHardAPI.csproj GoHardAPI/
RUN dotnet restore GoHardAPI/GoHardAPI.csproj

# Copy everything else and build (v2 - force rebuild)
COPY GoHardAPI/ GoHardAPI/
WORKDIR /src/GoHardAPI
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Use shell form to evaluate PORT environment variable at runtime
CMD ASPNETCORE_URLS=http://*:${PORT:-8080} dotnet GoHardAPI.dll
