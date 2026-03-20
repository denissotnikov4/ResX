FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Services/Identity/ResX.Identity.API/ResX.Identity.API.csproj", "Services/Identity/ResX.Identity.API/"]
COPY ["Services/Identity/ResX.Identity.Application/ResX.Identity.Application.csproj", "Services/Identity/ResX.Identity.Application/"]
COPY ["Services/Identity/ResX.Identity.Domain/ResX.Identity.Domain.csproj", "Services/Identity/ResX.Identity.Domain/"]
COPY ["Services/Identity/ResX.Identity.Infrastructure/ResX.Identity.Infrastructure.csproj", "Services/Identity/ResX.Identity.Infrastructure/"]
COPY ["BuildingBlocks/ResX.Common/ResX.Common.csproj", "BuildingBlocks/ResX.Common/"]
COPY ["BuildingBlocks/ResX.EventBus.RabbitMQ/ResX.EventBus.RabbitMQ.csproj", "BuildingBlocks/ResX.EventBus.RabbitMQ/"]
COPY ["BuildingBlocks/ResX.Caching.Redis/ResX.Caching.Redis.csproj", "BuildingBlocks/ResX.Caching.Redis/"]
COPY ["Protos/", "Protos/"]

RUN dotnet restore "Services/Identity/ResX.Identity.API/ResX.Identity.API.csproj"

COPY . .
WORKDIR "/src/Services/Identity/ResX.Identity.API"
RUN dotnet build "ResX.Identity.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "ResX.Identity.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResX.Identity.API.dll"]
