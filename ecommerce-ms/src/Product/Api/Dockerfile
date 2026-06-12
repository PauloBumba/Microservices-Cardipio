FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Directory.Packages.props .
COPY ECommerce.sln .
COPY src/Product/Domain/Product.Domain.csproj                 src/Product/Domain/
COPY src/Product/Application/Product.Application.csproj       src/Product/Application/
COPY src/Product/Infrastructure/Product.Infrastructure.csproj src/Product/Infrastructure/
COPY src/Product/Api/Product.Api.csproj                       src/Product/Api/
RUN dotnet restore src/Product/Api/Product.Api.csproj
COPY . .
RUN dotnet publish src/Product/Api/Product.Api.csproj -c Release -o /app/publish /p:UseAppHost=false
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Product.Api.dll"]
