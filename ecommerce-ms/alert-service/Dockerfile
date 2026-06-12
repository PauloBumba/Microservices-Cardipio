FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/AlertService.API/AlertService.API.csproj", "AlertService.API/"]
COPY ["src/AlertService.Core/AlertService.Core.csproj", "AlertService.Core/"]
COPY ["src/AlertService.Infrastructure/AlertService.Infrastructure.csproj", "AlertService.Infrastructure/"]

RUN dotnet restore "AlertService.API/AlertService.API.csproj"

COPY src/ .
RUN dotnet publish "AlertService.API/AlertService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AlertService.API.dll"]
