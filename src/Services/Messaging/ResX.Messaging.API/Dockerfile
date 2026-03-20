FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Services/Messaging/ResX.Messaging.API/ResX.Messaging.API.csproj", "Services/Messaging/ResX.Messaging.API/"]
COPY ["Services/Messaging/ResX.Messaging.Application/ResX.Messaging.Application.csproj", "Services/Messaging/ResX.Messaging.Application/"]
COPY ["Services/Messaging/ResX.Messaging.Domain/ResX.Messaging.Domain.csproj", "Services/Messaging/ResX.Messaging.Domain/"]
COPY ["Services/Messaging/ResX.Messaging.Infrastructure/ResX.Messaging.Infrastructure.csproj", "Services/Messaging/ResX.Messaging.Infrastructure/"]
COPY ["BuildingBlocks/ResX.Common/ResX.Common.csproj", "BuildingBlocks/ResX.Common/"]
COPY ["BuildingBlocks/ResX.EventBus.RabbitMQ/ResX.EventBus.RabbitMQ.csproj", "BuildingBlocks/ResX.EventBus.RabbitMQ/"]

RUN dotnet restore "Services/Messaging/ResX.Messaging.API/ResX.Messaging.API.csproj"
COPY . .
WORKDIR "/src/Services/Messaging/ResX.Messaging.API"
RUN dotnet build "ResX.Messaging.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "ResX.Messaging.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResX.Messaging.API.dll"]
