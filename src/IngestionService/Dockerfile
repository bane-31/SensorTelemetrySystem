FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5128

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/IngestionService/IngestionService.csproj", "src/IngestionService/"]
COPY ["src/SharedContracts/SharedContracts.csproj", "src/SharedContracts/"]

RUN dotnet restore "src/IngestionService/IngestionService.csproj"

COPY src/IngestionService/ src/IngestionService/
COPY src/SharedContracts/ src/SharedContracts/

WORKDIR "/src/src/IngestionService"
RUN dotnet build "IngestionService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IngestionService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IngestionService.dll"]