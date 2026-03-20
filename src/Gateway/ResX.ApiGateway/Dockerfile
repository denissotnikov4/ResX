FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Gateway/ResX.ApiGateway/ResX.ApiGateway.csproj", "Gateway/ResX.ApiGateway/"]
RUN dotnet restore "Gateway/ResX.ApiGateway/ResX.ApiGateway.csproj"
COPY . .
WORKDIR "/src/Gateway/ResX.ApiGateway"
RUN dotnet publish "ResX.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ResX.ApiGateway.dll"]
