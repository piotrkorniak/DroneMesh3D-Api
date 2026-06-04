# Implementation Plan: Definicja Obszaru na Mapie

## Overview

Implementacja przebiega w etapach: najpierw struktura projektu i modele danych, następnie logika walidacji (frontend i backend), potem komponenty UI z OpenLayers, a na końcu integracja warstw i testy end-to-end.

**Stos technologiczny:**
- **Frontend:** Angular 21 (standalone components, signals, signal inputs/outputs, inject(), nowy control flow @if/@for, zoneless change detection)
- **Backend:** .NET 10, Minimal API, MediatR (wzorzec Mediator/CQRS), FluentValidation, EF Core 10 z NetTopologySuite
- **Baza danych:** PostgreSQL 18 z PostGIS (w kontenerze Docker) z typem GEOMETRY(Polygon, 4326)
- **Infrastruktura:** Docker Compose (3 kontenery: frontend/nginx, api/.NET, db/PostgreSQL+PostGIS), hot-reload w development
- **Pakiety dodatkowe:** MediatR, FluentValidation, OneOf (discriminated unions), Npgsql.EntityFrameworkCore.PostgreSQL, NetTopologySuite

## Tasks

- [ ] 1. Konfiguracja struktury projektu, Docker i modeli danych
  - [ ] 1.1 Utworzenie struktury Docker Compose
    - Utworzenie `docker-compose.yml` z trzema serwisami: `frontend` (Angular/nginx), `api` (.NET 10), `db` (PostgreSQL 18 + PostGIS)
    - Utworzenie `docker-compose.override.yml` dla development (hot-reload, volume mounts)
    - Utworzenie `backend/Dockerfile` (multi-stage: sdk → aspnet)
    - Utworzenie `frontend/Dockerfile` (multi-stage: node → nginx)
    - Utworzenie `frontend/Dockerfile.dev` (node z npm start)
    - Utworzenie `frontend/nginx.conf` (reverse proxy /api/ → api:8080)
    - Weryfikacja: `docker compose up` startuje wszystkie 3 kontenery, PostgreSQL zdrowy (healthcheck pg_isready)
    - _Wymagania: infrastruktura_

  - [ ] 1.2 Konfiguracja CI/CD (GitHub Actions)
    - Utworzenie `.github/workflows/ci.yml` z trzema jobami: format-check, test, build
    - Job `format-check`: CleanupCode (dry-run + git diff --exit-code) dla backendu, Prettier --check + ESLint dla frontendu
    - Job `test`: uruchomienie testów .NET (dotnet test) z PostgreSQL/PostGIS service container, testów Angular (ChromeHeadless)
    - Job `build`: budowanie obrazów Docker (api + frontend)
    - Trigger: pull requesty do main
    - _Wymagania: infrastruktura_

  - [ ] 1.3 Konfiguracja formatowania kodu
    - Backend: zainstalowanie `JetBrains.ReSharper.GlobalTools` jako dotnet tool, utworzenie `.editorconfig` z regułami C# (file-scoped namespaces, primary constructors, sortowanie usings)
    - Frontend: zainstalowanie Prettier i ESLint, utworzenie `.prettierrc` i `eslint.config.js` (flat config Angular 21)
    - Dodanie skryptów npm: `format`, `format:check`, `lint`
    - Weryfikacja: `jb cleanupcode` i `npx prettier --check .` przechodzą czysto
    - _Wymagania: infrastruktura_

  - [ ] 1.4 Utworzenie projektu Angular 21 z zależnościami OpenLayers
    - Zainicjalizowanie aplikacji Angular 21 (standalone bootstrap, zoneless)
    - Zainstalowanie pakietów `ol` (OpenLayers)
    - Skonfigurowanie `angular.json` z globalnym CSS dla OpenLayers
    - Utworzenie plików modeli: `src/app/models/geojson.ts` i `src/app/models/validation.ts` z interfejsami `GeoJsonPolygon`, `ValidationResult`, `ValidationError` i enum `ValidationRule`
    - _Wymagania: 1.1, 4.2, 4.3_

  - [ ] 1.5 Utworzenie projektu .NET 10 Web API z MediatR, EF Core i typami przestrzennymi
    - Zainicjalizowanie projektu ASP.NET Core Web API (.NET 10, file-scoped namespaces, nullable reference types, global usings)
    - Zainstalowanie pakietów: `MediatR`, `FluentValidation.DependencyInjectionExtensions`, `OneOf`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `NetTopologySuite`, `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite`, `Scalar.AspNetCore`
    - Konfiguracja OpenAPI + Scalar w Program.cs (`AddOpenApi`, `MapOpenApi`, `MapScalarApiReference`)
    - Utworzenie encji `AreaEntity` (sealed class, Id Guid, CreatedAt DateTimeOffset, required Geometry NTS Geometry)
    - Utworzenie `AppDbContext` z primary constructor, konfiguracją PostGIS (`HasPostgresExtension("postgis")`) i typem `geometry(Polygon, 4326)`
    - Utworzenie DTO jako records: `CreateAreaRequest`, `AreaResponse`, `GeoJsonGeometry`, `ErrorResponse`, `ValidationErrorResponse`
    - Utworzenie interfejsów: `IAreaValidator`, `IAreaRepository`
    - Konfiguracja `Program.cs`: MediatR + ValidationBehavior + FluentValidation + EF Core (Npgsql + UseNetTopologySuite) + CORS + OpenAPI/Scalar
    - _Wymagania: 5.1, 6.1, 6.2, 6.3_

  - [ ] 1.6 Utworzenie migracji bazy danych dla tabeli Areas
    - Dodanie migracji EF Core tworzącej tabelę `Areas` z kolumnami `Id` (UUID, PK), `CreatedAt` (TIMESTAMPTZ), `Geometry` (GEOMETRY(Polygon, 4326))
    - Migracja aktywuje rozszerzenie PostGIS (`CREATE EXTENSION IF NOT EXISTS postgis`)
    - Dodanie GIST index na kolumnie `Geometry`
    - _Wymagania: 6.2, 6.3_

- [ ] 2. Implementacja walidacji poligonów na frontendzie
  - [ ] 2.1 Implementacja `PolygonValidatorService`
    - Utworzenie serwisu `src/app/services/polygon-validator.service.ts` (injectable, providedIn: root)
    - Implementacja metod: `validate()`, `hasMinVertices()`, `isClosed()`, `hasSelfIntersection()`, `calculateAreaSqm()`
    - Algorytm samoprzecięć: sprawdzenie przecięć krawędzi nie-sąsiadujących (segment intersection)
    - Obliczanie powierzchni: formuła sferyczna dla współrzędnych geograficznych
    - Stałe: `MAX_AREA_HECTARES = 5`, `MIN_AREA_SQM = 100`
    - _Wymagania: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [ ]* 2.2 Testy property-based dla walidacji liczby wierzchołków
    - **Property 1: Walidacja liczby wierzchołków**
    - Generowanie losowych tablic współrzędnych z <3 różnymi wierzchołkami → oczekiwany błąd MIN_VERTICES
    - Generowanie losowych tablic z ≥3 różnymi wierzchołkami → brak błędu MIN_VERTICES
    - **Waliduje: Wymaganie 3.1**

  - [ ]* 2.3 Testy property-based dla walidacji zamknięcia poligonu
    - **Property 2: Walidacja zamknięcia poligonu**
    - Generowanie tablic, w których pierwsza ≠ ostatnia współrzędna → oczekiwany błąd CLOSURE
    - Generowanie tablic, w których pierwsza = ostatnia współrzędna → brak błędu CLOSURE
    - **Waliduje: Wymaganie 3.2**

  - [ ]* 2.4 Testy property-based dla wykrywania samoprzecięć
    - **Property 3: Wykrywanie samoprzecięć**
    - Generowanie samoprzecinających się poligonów → oczekiwany błąd SELF_INTERSECTION
    - Generowanie prostych (nie-samoprzecinających się) poligonów → brak błędu SELF_INTERSECTION
    - **Waliduje: Wymaganie 3.3**

  - [ ]* 2.5 Testy property-based dla limitów powierzchni
    - **Property 4: Walidacja limitów powierzchni**
    - Generowanie poligonów o powierzchni >50 000 m² → oczekiwany błąd AREA_TOO_LARGE
    - Generowanie poligonów o powierzchni <100 m² → oczekiwany błąd AREA_TOO_SMALL
    - Generowanie poligonów o powierzchni w przedziale [100, 50 000] m² → brak błędu dotyczącego powierzchni
    - **Waliduje: Wymagania 3.4, 3.5**

  - [ ]* 2.6 Testy jednostkowe dla `PolygonValidatorService`
    - Test: poligon z 2 wierzchołkami → błąd MIN_VERTICES
    - Test: poligon niezamknięty → błąd CLOSURE
    - Test: motylkowy kształt → błąd SELF_INTERSECTION
    - Test: prawidłowy poligon → isValid = true
    - _Wymagania: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 3. Implementacja walidacji i MediatR na backendzie
  - [ ] 3.1 Implementacja `AreaValidator`
    - Utworzenie klasy `AreaValidator : IAreaValidator` (sealed, z metodami: `Validate()`, `HasMinimumVertices()`, `IsClosed()`, `HasSelfIntersection()`, `CalculateAreaSqm()`)
    - Identyczne reguły jak na frontendzie: ≥3 wierzchołki, zamknięcie, brak samoprzecięć, powierzchnia w [100 m², 50 000 m²]
    - Rejestracja w DI jako scoped
    - _Wymagania: 5.4, 5.5_

  - [ ] 3.2 Implementacja `GeoJsonValidator` (walidacja struktury)
    - Utworzenie statycznej klasy walidującej strukturę GeoJSON: obecność pola `type` = "Polygon", obecność `coordinates`, numeryczne współrzędne, niepusty pierścień
    - _Wymagania: 5.2, 5.3_

  - [ ] 3.3 Implementacja `CreateAreaCommand` i `CreateAreaCommandHandler`
    - Utworzenie record `CreateAreaCommand : IRequest<OneOf<AreaResponse, ValidationErrorResponse, ErrorResponse>>`
    - Utworzenie handlera z primary constructor (inject: IAreaValidator, IAreaRepository)
    - Przepływ: walidacja GeoJSON → odrzucenie multi-ring (holes) → walidacja geometrii → konwersja NTS → persystencja → mapowanie na AreaResponse
    - _Wymagania: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 6.1_

  - [ ] 3.4 Implementacja `GetAreaQuery` i `GetAreaQueryHandler`
    - Utworzenie record `GetAreaQuery(Guid Id) : IRequest<AreaResponse?>`
    - Utworzenie handlera odczytującego z repozytorium i mapującego na AreaResponse
    - _Wymagania: 5.6_

  - [ ] 3.5 Implementacja `ValidationBehavior` (MediatR pipeline)
    - Utworzenie generic pipeline behavior integrującego FluentValidation z MediatR
    - Automatyczna walidacja requestów przed dotarciem do handlera
    - _Wymagania: 5.2, 5.4_

  - [ ] 3.6 Implementacja `AreasEndpoint` (Minimal API)
    - Utworzenie statycznej klasy z extension method `MapAreasEndpoints()`
    - POST /api/areas: odbiera request → tworzy command → wysyła przez IMediator → mapuje wynik na HTTP response (201/400/422)
    - GET /api/areas/{id}: tworzy query → wysyła przez IMediator → 200/404
    - _Wymagania: 5.1, 5.6_

  - [ ] 3.7 Implementacja Global Exception Handler (middleware)
    - Utworzenie middleware przechwytującego nieobsłużone wyjątki (w tym `ValidationException` z FluentValidation)
    - `ValidationException` → HTTP 400 z listą błędów
    - Pozostałe wyjątki → HTTP 500 z ogólnym komunikatem, logowanie szczegółów wewnętrznie
    - Rejestracja w pipeline (`app.UseExceptionHandler()` lub custom middleware)
    - _Wymagania: 5.3, 6.4_

  - [ ]* 3.8 Testy property-based dla walidacji struktury GeoJSON na backendzie
    - **Property 6: Backend odrzuca nieprawidłowy GeoJSON**
    - Generowanie losowych obiektów JSON niespełniających struktury GeoJSON Polygon → oczekiwany wynik: walidacja nieudana
    - **Waliduje: Wymagania 5.2, 5.3**

  - [ ]* 3.9 Testy property-based dla zgodności walidacji frontend-backend
    - **Property 7: Zgodność walidacji frontend-backend**
    - Generowanie losowych tablic współrzędnych → porównanie werdyktu AreaValidator (C#) z PolygonValidatorService (TS) — oba powinny dać identyczny wynik
    - **Waliduje: Wymagania 5.4, 5.5**

  - [ ]* 3.10 Testy jednostkowe dla `AreaValidator` i `CreateAreaCommandHandler`
    - Test handler: prawidłowy command → zwraca AreaResponse (OneOf case 1)
    - Test handler: nieprawidłowy GeoJSON → zwraca ErrorResponse (OneOf case 3)
    - Test handler: nieudana walidacja geometrii → zwraca ValidationErrorResponse (OneOf case 2)
    - Test validator: <3 wierzchołki, niezamknięty, samoprzecinający się, za duży, za mały
    - _Wymagania: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [ ] 4. Punkt kontrolny — Upewnij się, że testy walidacji przechodzą
  - Uruchomienie wszystkich testów frontendowych i backendowych
  - Zapytaj użytkownika w razie pytań

- [ ] 5. Implementacja komponentu mapy i rysowania poligonów
  - [ ] 5.1 Implementacja `MapComponent` z OpenLayers i signals
    - Utworzenie standalone component `MapComponent` z ChangeDetectionStrategy.OnPush
    - Użycie `inject()` zamiast constructor injection
    - Stan komponentu przez signals: `validationResult`, `isSubmitting`, `submissionError`, `hasPolygon`, `isDrawing`
    - Computed signals: `isValid`, `validationErrors`
    - Inicjalizacja mapy OpenLayers z warstwą satelitarną i projekcją EPSG:3857 (Web Mercator)
    - Transformacja współrzędnych z EPSG:3857 → EPSG:4326 przy komunikacji z API (ol/proj toLonLat)
    - VectorSource i VectorLayer dla poligonów
    - _Wymagania: 1.1, 1.2, 1.3_

  - [ ] 5.2 Implementacja interakcji rysowania poligonu
    - Dodanie Draw interaction z typem 'Polygon' do mapy
    - Aktywacja/dezaktywacja przez signal `isDrawing`
    - Wyświetlanie wierzchołków i krawędzi w czasie rzeczywistym
    - Zamknięcie poligonu przez double-click lub kliknięcie pierwszego wierzchołka
    - Wywołanie walidacji (PolygonValidatorService) po zakończeniu rysowania
    - _Wymagania: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [ ] 5.3 Implementacja edycji poligonu (Modify interaction)
    - Dodanie Modify interaction pozwalającej na przeciąganie wierzchołków
    - Re-walidacja geometrii po każdej modyfikacji (update signal validationResult)
    - _Wymagania: 7.1, 7.2, 7.3_

  - [ ] 5.4 Implementacja `MapToolbarComponent` z signal inputs
    - Utworzenie standalone component z `input()` i `output()` (Angular 21 API)
    - Signal inputs: `isDrawing`, `hasPolygon`, `isValid`, `isSubmitting`, `validationErrors`
    - Outputs: `draw`, `clear`, `submit`
    - Nowy control flow: `@if(isValid())` zamiast `*ngIf`
    - Stany przycisków oparte na computed logic
    - _Wymagania: 2.1, 2.6, 3.6, 3.7, 4.1_

  - [ ] 5.5 Implementacja wizualnego feedbacku walidacji
    - Podświetlenie poligonu na czerwono gdy walidacja nieudana
    - Styl domyślny (niebieski) gdy poligon prawidłowy
    - Reaktywna zmiana stylu na bazie signal `isValid`
    - _Wymagania: 3.6, 3.7_

- [ ] 6. Implementacja komunikacji z API i wysyłki
  - [ ] 6.1 Generowanie klienta HTTP z OpenAPI spec
    - Zainstalowanie `@openapitools/openapi-generator-cli` jako devDependency
    - Dodanie skryptu npm `api:generate` generującego klienta TypeScript-Angular z `http://localhost:5000/openapi/v1.json`
    - Wygenerowany kod trafia do `src/app/api/` (services, models)
    - Dodanie wygenerowanego katalogu do `.gitignore` lub commitowanie (decyzja: commitujemy dla CI)
    - _Wymagania: 4.4_

  - [ ] 6.2 Implementacja `AreaService` (wrapper nad wygenerowanym klientem)
    - Utworzenie serwisu z `inject()` delegującego do wygenerowanego `AreasApiService`
    - Metoda `createArea(geojson: CreateAreaRequest): Observable<AreaResponse>`
    - _Wymagania: 4.4_

  - [ ]* 6.3 Testy property-based dla zachowania współrzędnych w GeoJSON
    - **Property 5: Zachowanie współrzędnych w GeoJSON**
    - Generowanie prawidłowych tablic współrzędnych → konwersja do GeoJSON → sprawdzenie zachowania współrzędnych w oryginalnej kolejności
    - **Waliduje: Wymagania 4.2, 4.3**

  - [ ] 6.4 Implementacja logiki wysyłania w `MapComponent`
    - Metoda `submitArea()`: konwersja współrzędnych OL → GeoJSON, wywołanie AreaService
    - Zarządzanie stanem przez signals: `isSubmitting.set(true)`, dezaktywacja submit
    - Obsługa sukcesu/błędu z aktualizacją signals
    - _Wymagania: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [ ]* 6.5 Testy jednostkowe dla `AreaService`
    - Test: wywołanie POST z prawidłowym URL i ładunkiem (provideHttpClientTesting)
    - Test: obsługa błędu sieciowego
    - _Wymagania: 4.4, 4.6_

- [ ] 7. Implementacja repozytorium i persystencji
  - [ ] 7.1 Implementacja `AreaRepository`
    - Utworzenie sealed class z primary constructor (inject AppDbContext)
    - Metoda `AddAsync()` z CancellationToken
    - Metoda `GetByIdAsync()` z CancellationToken
    - Rejestracja w DI jako scoped
    - _Wymagania: 6.1, 6.2, 6.3_

  - [ ] 7.2 Implementacja `GeometryConverter` (helper)
    - Statyczna klasa konwertująca double[][] → NTS Polygon i NTS Polygon → GeoJsonGeometry
    - Ustawienie SRID = 4326 (WGS 84)
    - _Wymagania: 6.2_

  - [ ]* 7.3 Testy property-based dla round-trip persystencji
    - **Property 8: Round-trip persystencji obszaru**
    - Generowanie prawidłowych definicji → zapis → odczyt po ID → sprawdzenie identycznych współrzędnych, niepustego ID i prawidłowego timestamp
    - **Waliduje: Wymagania 6.1, 6.3**

  - [ ]* 7.4 Testy integracyjne endpointu z WebApplicationFactory
    - Test: POST prawidłowego poligonu → 201 Created z AreaResponse
    - Test: POST nieprawidłowego JSON → 400 Bad Request
    - Test: POST z nieudaną walidacją geometrii → 422 Unprocessable Entity
    - Test: GET /api/areas/{id} → 200 z prawidłowymi danymi
    - Test: GET /api/areas/{nonexistent} → 404
    - _Wymagania: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 6.4_

- [ ] 8. Punkt kontrolny — Upewnij się, że testy przechodzą
  - Uruchomienie pełnego zestawu testów
  - Zapytaj użytkownika w razie pytań

- [ ] 9. Integracja i połączenie warstw
  - [ ] 9.1 Konfiguracja proxy Angular do backendu .NET (development bez Dockera)
    - Utworzenie pliku `proxy.conf.json` przekierowującego `/api/*` na backend .NET (domyślnie https://localhost:5001)
    - Konfiguracja `angular.json` do użycia proxy w trybie dev (`ng serve`)
    - Uwaga: w środowisku Docker nginx pełni rolę reverse proxy — `proxy.conf.json` jest używany tylko przy `ng serve` poza kontenerem
    - _Wymagania: 4.4_

  - [ ] 9.2 Połączenie komponentów frontendowych w `AppComponent`
    - Osadzenie `MapComponent` jako głównego widoku
    - Weryfikacja pełnego przepływu: rysowanie → walidacja → wysyłka → odpowiedź z backendu
    - _Wymagania: 1.1, 2.1, 4.1_

  - [ ]* 9.3 Testy integracyjne end-to-end
    - Test manualny lub e2e: narysowanie poligonu → submit → 201 → dane w bazie
    - _Wymagania: wszystkie_

- [ ] 10. Punkt kontrolny końcowy
  - Uruchomienie wszystkich testów (frontend + backend + integracyjne)
  - Zapytaj użytkownika w razie pytań

## Notes

- Zadania oznaczone `*` są opcjonalne i mogą być pominięte dla szybszego MVP
- Każde zadanie referencjonuje konkretne wymagania dla śledzenia
- Punkty kontrolne zapewniają inkrementalną walidację
- **MediatR pattern**: kontroler/endpoint jest cienki — logika w handlerach, walidacja w pipeline behavior
- **Angular 21**: signals zamiast BehaviorSubject dla stanu komponentu, input()/output() zamiast @Input/@Output dekoratora, inject() zamiast constructor injection
- **.NET 10**: primary constructors dla klas, records dla DTO/commands/queries, Guid.CreateVersion7() dla deterministycznie sortowanych ID, sealed classes
- **OpenAPI**: backend eksponuje `/openapi/v1.json` z UI Scalar (`/scalar/v1`), frontend generuje klienta HTTP z `openapi-generator-cli` (typescript-angular). Żadne ręczne pisanie HTTP calls.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3", "1.4", "1.5"] },
    { "id": 2, "tasks": ["1.6", "2.1", "3.2"] },
    { "id": 3, "tasks": ["2.2", "2.3", "2.4", "2.5", "2.6", "3.1"] },
    { "id": 4, "tasks": ["3.3", "3.4", "3.5", "3.6", "3.7", "3.8", "3.9", "3.10"] },
    { "id": 5, "tasks": ["5.1", "6.1", "7.1", "7.2"] },
    { "id": 6, "tasks": ["5.2", "5.4", "6.2", "6.4"] },
    { "id": 7, "tasks": ["5.3", "5.5", "6.3", "6.5"] },
    { "id": 8, "tasks": ["7.3", "7.4"] },
    { "id": 9, "tasks": ["9.1", "9.2"] },
    { "id": 10, "tasks": ["9.3"] }
  ]
}
```
