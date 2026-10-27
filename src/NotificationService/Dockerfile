FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 5112

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/NotificationService/NotificationService.csproj", "src/NotificationService/"]
COPY ["src/SharedContracts/SharedContracts.csproj", "src/SharedContracts/"]

RUN dotnet restore "src/NotificationService/NotificationService.csproj"

COPY src/NotificationService/ src/NotificationService/
COPY src/SharedContracts/ src/SharedContracts/

WORKDIR "/src/src/NotificationService"
RUN dotnet build "NotificationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]