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

## Uputstvo za pokretanje
1. Klonirajte repozitorijum.
2. Podignite bazu komandom: `docker-compose up -d`.
3. Migrirajte bazu: `dotnet ef database update` (unutar `IngestionService` foldera).
4. Pokrenite servise: `dotnet run` (za svaki projekat pojedinačno).