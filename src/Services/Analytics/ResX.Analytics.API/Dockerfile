FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Analytics/ResX.Analytics.API/ResX.Analytics.API.csproj", "Services/Analytics/ResX.Analytics.API/"]
COPY ["Services/Analytics/ResX.Analytics.Application/ResX.Analytics.Application.csproj", "Services/Analytics/ResX.Analytics.Application/"]
COPY ["Services/Analytics/ResX.Analytics.Infrastructure/ResX.Analytics.Infrastructure.csproj", "Services/Analytics/ResX.Analytics.Infrastructure/"]
COPY ["BuildingBlocks/ResX.Common/ResX.Common.csproj", "BuildingBlocks/ResX.Common/"]
RUN dotnet restore "Services/Analytics/ResX.Analytics.API/ResX.Analytics.API.csproj"
COPY . .
WORKDIR "/src/Services/Analytics/ResX.Analytics.API"
RUN dotnet publish "ResX.Analytics.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ResX.Analytics.API.dll"]
