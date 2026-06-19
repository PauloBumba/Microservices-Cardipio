FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia os csproj para cache de restore
COPY src/Order/Reference/Order.Reference.csproj       src/Order/Reference/
COPY src/Order/Domain/Order.Domain.csproj             src/Order/Domain/
COPY src/Order/Application/Order.Application.csproj   src/Order/Application/
COPY src/Order/Infrastructure/Order.Infrastructure.csproj src/Order/Infrastructure/
COPY src/Order/Api/Order.Api.csproj                   src/Order/Api/

# Restore — só reinstala se .csproj mudar
RUN dotnet restore src/Order/Api/Order.Api.csproj

# Copia o restante do código
COPY src/Order/ src/Order/

RUN dotnet publish src/Order/Api/Order.Api.csproj     -c Release     -o /app/publish     /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Order.Api.dll"]
