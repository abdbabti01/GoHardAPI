FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY GoHardAPI/GoHardAPI.csproj GoHardAPI/
RUN dotnet restore GoHardAPI/GoHardAPI.csproj

# Copy everything else and build
COPY GoHardAPI/ GoHardAPI/
WORKDIR /src/GoHardAPI
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Railway provides PORT environment variable
ENV ASPNETCORE_URLS=http://+:$PORT

ENTRYPOINT ["dotnet", "GoHardAPI.dll"]
