# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore FataleCore.csproj
RUN dotnet publish FataleCore.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Create directories for persistent data, uploads, and cache
RUN mkdir -p /app/data /app/uploads /app/Cache

ENV ASPNETCORE_ENVIRONMENT=Production

# Railway injects PORT dynamically. We use a shell script as entrypoint so we can
# read $PORT at container-start time and pass it to ASP.NET Core via ASPNETCORE_URLS.
EXPOSE 8080

ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet FataleCore.dll"]
