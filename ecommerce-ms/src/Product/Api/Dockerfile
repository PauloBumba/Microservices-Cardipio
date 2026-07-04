# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy .csproj files for dependency cache
COPY src/Shared/Domain/Shared.Domain.csproj                 src/Shared/Domain/
COPY src/Shared/Application/Shared.Application.csproj       src/Shared/Application/
COPY src/Shared/Infrastructure/Shared.Infrastructure.csproj src/Shared/Infrastructure/
COPY src/Product/Reference/Product.Reference.csproj        src/Product/Reference/
COPY src/Product/Domain/Product.Domain.csproj              src/Product/Domain/
COPY src/Product/Application/Product.Application.csproj    src/Product/Application/
COPY src/Product/Infrastructure/Product.Infrastructure.csproj src/Product/Infrastructure/
COPY src/Product/Api/Product.Api.csproj                    src/Product/Api/

# Restore dependencies (cached layer)
RUN dotnet restore src/Product/Api/Product.Api.csproj

# Copy source code
COPY src/Shared/ src/Shared/
COPY src/Product/ src/Product/

# Publish
RUN dotnet publish src/Product/Api/Product.Api.csproj \
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
ENTRYPOINT ["dotnet", "Product.Api.dll"]
