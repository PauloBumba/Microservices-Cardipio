# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy .csproj files for dependency cache
COPY src/AlertService.Core/AlertService.Core.csproj             AlertService.Core/
COPY src/AlertService.Infrastructure/AlertService.Infrastructure.csproj AlertService.Infrastructure/
COPY src/AlertService.API/AlertService.API.csproj               AlertService.API/

# Restore dependencies (cached layer)
RUN dotnet restore AlertService.API/AlertService.API.csproj

# Copy source code
COPY src/ .

# Publish
RUN dotnet publish AlertService.API/AlertService.API.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Final stage
FROM runtime AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AlertService.API.dll"]
