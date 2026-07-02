# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy .csproj files for dependency cache
COPY src/Shared/Domain/Shared.Domain.csproj                         src/Shared/Domain/
COPY src/Shared/Application/Shared.Application.csproj               src/Shared/Application/
COPY src/Shared/Infrastructure/Shared.Infrastructure.csproj         src/Shared/Infrastructure/
COPY src/Notification/Reference/Notification.Reference.csproj       src/Notification/Reference/
COPY src/Notification/Domain/Notification.Domain.csproj             src/Notification/Domain/
COPY src/Notification/Application/Notification.Application.csproj   src/Notification/Application/
COPY src/Notification/Infrastructure/Notification.Infrastructure.csproj src/Notification/Infrastructure/
COPY src/Notification/Api/Notification.Api.csproj                   src/Notification/Api/

# Restore dependencies (cached layer)
RUN dotnet restore src/Notification/Api/Notification.Api.csproj

# Copy source code
COPY src/Shared/ src/Shared/
COPY src/Notification/ src/Notification/

# Publish
RUN dotnet publish src/Notification/Api/Notification.Api.csproj \
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
ENTRYPOINT ["dotnet", "Notification.Api.dll"]
