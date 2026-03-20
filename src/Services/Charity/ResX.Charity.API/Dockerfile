FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Charity/ResX.Charity.API/ResX.Charity.API.csproj", "Services/Charity/ResX.Charity.API/"]
COPY ["Services/Charity/ResX.Charity.Application/ResX.Charity.Application.csproj", "Services/Charity/ResX.Charity.Application/"]
COPY ["Services/Charity/ResX.Charity.Domain/ResX.Charity.Domain.csproj", "Services/Charity/ResX.Charity.Domain/"]
COPY ["Services/Charity/ResX.Charity.Infrastructure/ResX.Charity.Infrastructure.csproj", "Services/Charity/ResX.Charity.Infrastructure/"]
COPY ["BuildingBlocks/ResX.Common/ResX.Common.csproj", "BuildingBlocks/ResX.Common/"]
RUN dotnet restore "Services/Charity/ResX.Charity.API/ResX.Charity.API.csproj"
COPY . .
WORKDIR "/src/Services/Charity/ResX.Charity.API"
RUN dotnet publish "ResX.Charity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM base AS final
WORKDIR /app
COPY --from=0 /app/publish .
ENTRYPOINT ["dotnet", "ResX.Charity.API.dll"]
