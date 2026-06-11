FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Directory.Packages.props .
COPY ECommerce.sln .
COPY src/Notification/Domain/Notification.Domain.csproj                 src/Notification/Domain/
COPY src/Notification/Application/Notification.Application.csproj       src/Notification/Application/
COPY src/Notification/Infrastructure/Notification.Infrastructure.csproj src/Notification/Infrastructure/
COPY src/Notification/Api/Notification.Api.csproj                       src/Notification/Api/
RUN dotnet restore src/Notification/Api/Notification.Api.csproj
COPY . .
RUN dotnet publish src/Notification/Api/Notification.Api.csproj -c Release -o /app/publish /p:UseAppHost=false
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Notification.Api.dll"]
