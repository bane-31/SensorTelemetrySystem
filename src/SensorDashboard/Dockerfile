FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/SensorDashboard/SensorDashboard.csproj", "src/SensorDashboard/"]
COPY ["src/SharedContracts/SharedContracts.csproj", "src/SharedContracts/"]

RUN dotnet restore "src/SensorDashboard/SensorDashboard.csproj"

COPY src/SensorDashboard/ src/SensorDashboard/
COPY src/SharedContracts/ src/SharedContracts/

WORKDIR "/src/src/SensorDashboard"
RUN dotnet build "SensorDashboard.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SensorDashboard.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SensorDashboard.dll"]