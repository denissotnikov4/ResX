FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/Disputes/ResX.Disputes.API/ResX.Disputes.API.csproj", "Services/Disputes/ResX.Disputes.API/"]
COPY ["Services/Disputes/ResX.Disputes.Application/ResX.Disputes.Application.csproj", "Services/Disputes/ResX.Disputes.Application/"]
COPY ["Services/Disputes/ResX.Disputes.Domain/ResX.Disputes.Domain.csproj", "Services/Disputes/ResX.Disputes.Domain/"]
COPY ["Services/Disputes/ResX.Disputes.Infrastructure/ResX.Disputes.Infrastructure.csproj", "Services/Disputes/ResX.Disputes.Infrastructure/"]
COPY ["BuildingBlocks/ResX.Common/ResX.Common.csproj", "BuildingBlocks/ResX.Common/"]
RUN dotnet restore "Services/Disputes/ResX.Disputes.API/ResX.Disputes.API.csproj"
COPY . .
WORKDIR "/src/Services/Disputes/ResX.Disputes.API"
RUN dotnet publish "ResX.Disputes.API.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ResX.Disputes.API.dll"]
