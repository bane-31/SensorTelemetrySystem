FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/ConsensusService/ConsensusService.csproj", "src/ConsensusService/"]
COPY ["src/SharedContracts/SharedContracts.csproj", "src/SharedContracts/"]
COPY ["src/IngestionService/IngestionService.csproj", "src/IngestionService/"]

RUN dotnet restore "src/ConsensusService/ConsensusService.csproj"

COPY src/ConsensusService/ src/ConsensusService/
COPY src/SharedContracts/ src/SharedContracts/
COPY src/IngestionService/ src/IngestionService/

WORKDIR "/src/src/ConsensusService"
RUN dotnet build "ConsensusService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsensusService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsensusService.dll"]