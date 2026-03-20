FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Services/Notifications/ResX.Notifications.API/ResX.Notifications.API.csproj", "Services/Notifications/ResX.Notifications.API/"]
COPY ["Services/Notifications/ResX.Notifications.Application/ResX.Notifications.Application.csproj", "Services/Notifications/ResX.Notifications.Application/"]
COPY ["Services/Notifications/ResX.Notifications.Domain/ResX.Notifications.Domain.csproj", "Services/Notifications/ResX.Notifications.Domain/"]
COPY ["Services/Notifications/ResX.Notifications.Infrastructure/ResX.Notifications.Infrastructure.csproj", "Services/Notifications/ResX.Notifications.Infrastructure/"]
COPY ["BuildingBlocks/ResX.Common/ResX.Common.csproj", "BuildingBlocks/ResX.Common/"]
COPY ["BuildingBlocks/ResX.EventBus.RabbitMQ/ResX.EventBus.RabbitMQ.csproj", "BuildingBlocks/ResX.EventBus.RabbitMQ/"]

RUN dotnet restore "Services/Notifications/ResX.Notifications.API/ResX.Notifications.API.csproj"
COPY . .
WORKDIR "/src/Services/Notifications/ResX.Notifications.API"
RUN dotnet build "ResX.Notifications.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "ResX.Notifications.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResX.Notifications.API.dll"]
