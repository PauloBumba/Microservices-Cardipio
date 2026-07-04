# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy .csproj files for dependency cache
COPY src/Shared/Domain/Shared.Domain.csproj                 src/Shared/Domain/
COPY src/Shared/Application/Shared.Application.csproj       src/Shared/Application/
COPY src/Shared/Infrastructure/Shared.Infrastructure.csproj src/Shared/Infrastructure/
COPY src/Order/Reference/Order.Reference.csproj            src/Order/Reference/
COPY src/Order/Domain/Order.Domain.csproj                  src/Order/Domain/
COPY src/Order/Application/Order.Application.csproj        src/Order/Application/
COPY src/Order/Infrastructure/Order.Infrastructure.csproj  src/Order/Infrastructure/
COPY src/Order/Api/Order.Api.csproj                        src/Order/Api/

# Restore dependencies (cached layer)
RUN dotnet restore src/Order/Api/Order.Api.csproj

# Copy source code
COPY src/Shared/ src/Shared/
COPY src/Order/ src/Order/

# Publish
RUN dotnet publish src/Order/Api/Order.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Final stage
FROM runtime AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Order.Api.dll"]
