FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/SensorClient/SensorClient.csproj", "src/SensorClient/"]
COPY ["src/SharedContracts/SharedContracts.csproj", "src/SharedContracts/"]

RUN dotnet restore "src/SensorClient/SensorClient.csproj"

COPY src/SensorClient/ src/SensorClient/
COPY src/SharedContracts/ src/SharedContracts/

WORKDIR "/src/src/SensorClient"
RUN dotnet publish "SensorClient.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SensorClient.dll"]