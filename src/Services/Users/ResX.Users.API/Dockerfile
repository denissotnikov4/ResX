FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Services/Users/ResX.Users.API/ResX.Users.API.csproj", "Services/Users/ResX.Users.API/"]
COPY ["Services/Users/ResX.Users.Application/ResX.Users.Application.csproj", "Services/Users/ResX.Users.Application/"]
COPY ["Services/Users/ResX.Users.Domain/ResX.Users.Domain.csproj", "Services/Users/ResX.Users.Domain/"]
COPY ["Services/Users/ResX.Users.Infrastructure/ResX.Users.Infrastructure.csproj", "Services/Users/ResX.Users.Infrastructure/"]
COPY ["BuildingBlocks/ResX.Common/ResX.Common.csproj", "BuildingBlocks/ResX.Common/"]
COPY ["BuildingBlocks/ResX.EventBus.RabbitMQ/ResX.EventBus.RabbitMQ.csproj", "BuildingBlocks/ResX.EventBus.RabbitMQ/"]
COPY ["BuildingBlocks/ResX.Caching.Redis/ResX.Caching.Redis.csproj", "BuildingBlocks/ResX.Caching.Redis/"]
COPY ["Protos/", "Protos/"]

RUN dotnet restore "Services/Users/ResX.Users.API/ResX.Users.API.csproj"
COPY . .
WORKDIR "/src/Services/Users/ResX.Users.API"
RUN dotnet build "ResX.Users.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "ResX.Users.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResX.Users.API.dll"]
