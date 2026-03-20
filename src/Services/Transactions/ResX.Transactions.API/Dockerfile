FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Services/Transactions/ResX.Transactions.API/ResX.Transactions.API.csproj", "Services/Transactions/ResX.Transactions.API/"]
COPY ["Services/Transactions/ResX.Transactions.Application/ResX.Transactions.Application.csproj", "Services/Transactions/ResX.Transactions.Application/"]
COPY ["Services/Transactions/ResX.Transactions.Domain/ResX.Transactions.Domain.csproj", "Services/Transactions/ResX.Transactions.Domain/"]
COPY ["Services/Transactions/ResX.Transactions.Infrastructure/ResX.Transactions.Infrastructure.csproj", "Services/Transactions/ResX.Transactions.Infrastructure/"]
COPY ["BuildingBlocks/ResX.Common/ResX.Common.csproj", "BuildingBlocks/ResX.Common/"]
COPY ["BuildingBlocks/ResX.EventBus.RabbitMQ/ResX.EventBus.RabbitMQ.csproj", "BuildingBlocks/ResX.EventBus.RabbitMQ/"]

RUN dotnet restore "Services/Transactions/ResX.Transactions.API/ResX.Transactions.API.csproj"
COPY . .
WORKDIR "/src/Services/Transactions/ResX.Transactions.API"
RUN dotnet build "ResX.Transactions.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "ResX.Transactions.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResX.Transactions.API.dll"]
