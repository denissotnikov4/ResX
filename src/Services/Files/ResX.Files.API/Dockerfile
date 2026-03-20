FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Services/Files/ResX.Files.API/ResX.Files.API.csproj", "Services/Files/ResX.Files.API/"]
COPY ["Services/Files/ResX.Files.Application/ResX.Files.Application.csproj", "Services/Files/ResX.Files.Application/"]
COPY ["Services/Files/ResX.Files.Domain/ResX.Files.Domain.csproj", "Services/Files/ResX.Files.Domain/"]
COPY ["Services/Files/ResX.Files.Infrastructure/ResX.Files.Infrastructure.csproj", "Services/Files/ResX.Files.Infrastructure/"]
COPY ["BuildingBlocks/ResX.Common/ResX.Common.csproj", "BuildingBlocks/ResX.Common/"]
COPY ["BuildingBlocks/ResX.Storage.S3/ResX.Storage.S3.csproj", "BuildingBlocks/ResX.Storage.S3/"]
COPY ["Protos/", "Protos/"]

RUN dotnet restore "Services/Files/ResX.Files.API/ResX.Files.API.csproj"
COPY . .
WORKDIR "/src/Services/Files/ResX.Files.API"
RUN dotnet build "ResX.Files.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "ResX.Files.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResX.Files.API.dll"]
