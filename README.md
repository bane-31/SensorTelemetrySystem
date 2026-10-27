# Distribuirani sistem za nadzor temperature

## Opis sistema
Sistem je dizajniran kao distribuirana mikroservisna arhitektura namenjena visoko-dostupnom nadzoru telemetrijskih podataka (temperatura) u realnom vremenu. Sistem podržava autonomno dodavanje uređaja (Device Provisioning), detekciju anomalija, zaštitu od mrežnih napada i "self-healing" mehanizme za održavanje kvote aktivnih senzora.

## Arhitektura
* **IngestionService:** API zadužen za prijem telemetrije, validaciju, zaštitu od replay napada i atomski upis u bazu.
* **ConsensusService:** (U izradi) Analizira podatke i primenjuje BFT algoritam za detekciju lažnih podataka.
* **NotificationService:** (U izradi) Zadužen za prosleđivanje alarma operaterima putem SignalR protokola.

## Tehnologije
* **Backend:** .NET 8 (ASP.NET Core Web API)
* **Baza podataka:** PostgreSQL
* **Infrastruktura:** Docker, Kubernetes (u pripremi)
* **Komunikacija:** REST/JSON (interna), SignalR (eksterna)

## Development Setup
1. **Prerekviziti:**
   * Instaliran .NET 8 SDK.
   * Instaliran Docker Desktop.
   * Instaliran EF Core CLI (`dotnet tool install --global dotnet-ef`).

2. **Baza podataka:**
   * U korenskom folderu pokrenite: `docker-compose up -d`.
   * Ovo će podići PostgreSQL instancu na portu 5432.

3. **Migracije:**
   * Pozicionirajte se u `src/IngestionService/`.
   * Pokrenite: `dotnet ef database update`.

4. **Konfiguracija:**
   * U `IngestionService/appsettings.Development.json` proverite da li je Connection String ispravan (obično: `Host=localhost;Database=sensordb;Username=admin;Password=password`).
