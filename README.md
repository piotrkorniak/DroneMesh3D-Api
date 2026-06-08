# DroneMesh3D API

Backend .NET 10 do aplikacji DroneMesh3D — planowanie tras lotów dronów i generowanie plików misji.

## Wymagania

- .NET 10 SDK
- PostgreSQL 17 z PostGIS
- Docker (opcjonalnie)

## Start

```bash
# Baza danych (Docker)
docker compose up db -d

# API
dotnet run --project Api
```

API dostępne pod `http://localhost:5000`. Dokumentacja: `http://localhost:5000/scalar/v1`.

## Struktura projektu

| Folder | Rola |
|--------|------|
| `Api/` | Warstwa HTTP — endpointy, MediatR, walidacja |
| `Core/` | Logika biznesowa, EF Core, algorytmy lotu |
| `Tests/` | Testy jednostkowe i integracyjne (xUnit) |

## Endpointy

| Metoda | Ścieżka | Opis |
|--------|---------|------|
| GET | `/api/areas` | Lista obszarów |
| POST | `/api/areas` | Utwórz obszar (GeoJSON) |
| GET | `/api/areas/{id}` | Pobierz obszar |
| DELETE | `/api/areas/{id}` | Usuń obszar |
| GET | `/api/flight-plans` | Lista planów lotu |
| POST | `/api/flight-plans` | Oblicz trasę lotu |
| GET | `/api/flight-plans/{id}` | Pobierz plan lotu |
| GET | `/api/flight-plans/{id}/export?format=` | Eksport pliku misji (LitchiCsv, Kml, DjiWpml) |
| DELETE | `/api/flight-plans/{id}` | Usuń plan lotu |

## Docker

```bash
# Cały stack (API + DB + Web)
docker compose up

# Tylko backend + baza
docker compose up api db
```

API w kontenerze nasłuchuje na porcie `8080`, wystawione na hoście jako `5000`.

## Konfiguracja

`appsettings.json`:
- `ConnectionStrings:Default` — connection string do PostgreSQL/PostGIS
- `Cors:AllowedOrigins` — dozwolone originy (domyślnie `http://localhost:4200`)

## Stack

- .NET 10 / C# 14
- MediatR + FluentValidation
- EF Core 10 + NetTopologySuite + PostGIS
- Scalar (OpenAPI docs)
- xUnit + WebApplicationFactory
