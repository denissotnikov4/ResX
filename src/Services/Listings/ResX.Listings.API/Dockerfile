FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Services/Listings/ResX.Listings.API/ResX.Listings.API.csproj", "Services/Listings/ResX.Listings.API/"]
COPY ["Services/Listings/ResX.Listings.Application/ResX.Listings.Application.csproj", "Services/Listings/ResX.Listings.Application/"]
COPY ["Services/Listings/ResX.Listings.Domain/ResX.Listings.Domain.csproj", "Services/Listings/ResX.Listings.Domain/"]
COPY ["Services/Listings/ResX.Listings.Infrastructure/ResX.Listings.Infrastructure.csproj", "Services/Listings/ResX.Listings.Infrastructure/"]
COPY ["BuildingBlocks/ResX.Common/ResX.Common.csproj", "BuildingBlocks/ResX.Common/"]
COPY ["BuildingBlocks/ResX.EventBus.RabbitMQ/ResX.EventBus.RabbitMQ.csproj", "BuildingBlocks/ResX.EventBus.RabbitMQ/"]
COPY ["BuildingBlocks/ResX.Caching.Redis/ResX.Caching.Redis.csproj", "BuildingBlocks/ResX.Caching.Redis/"]
COPY ["Protos/", "Protos/"]

RUN dotnet restore "Services/Listings/ResX.Listings.API/ResX.Listings.API.csproj"

COPY . .
WORKDIR "/src/Services/Listings/ResX.Listings.API"
RUN dotnet build "ResX.Listings.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "ResX.Listings.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResX.Listings.API.dll"]
