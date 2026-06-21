FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# Copia os csproj para cache de restore
COPY src/Shared/Domain/Shared.Domain.csproj                 src/Shared/Domain/
COPY src/Shared/Application/Shared.Application.csproj       src/Shared/Application/
COPY src/Shared/Infrastructure/Shared.Infrastructure.csproj src/Shared/Infrastructure/
COPY src/Customer/Reference/Customer.Reference.csproj       src/Customer/Reference/
COPY src/Customer/Domain/Customer.Domain.csproj             src/Customer/Domain/
COPY src/Customer/Application/Customer.Application.csproj   src/Customer/Application/
COPY src/Customer/Infrastructure/Customer.Infrastructure.csproj src/Customer/Infrastructure/
COPY src/Customer/Api/Customer.Api.csproj                   src/Customer/Api/
# Restore — só reinstala se .csproj mudar
RUN dotnet restore src/Customer/Api/Customer.Api.csproj
# Copia o restante do código
COPY src/Shared/ src/Shared/
COPY src/Customer/ src/Customer/
RUN dotnet publish src/Customer/Api/Customer.Api.csproj     -c Release     -o /app/publish     /p:UseAppHost=false
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Customer.Api.dll"]