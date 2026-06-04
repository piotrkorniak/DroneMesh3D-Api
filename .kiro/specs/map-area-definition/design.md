# Design Document

## Overview

Ten projekt implementuje funkcjonalność Definicji Obszaru na Mapie dla DroneMesh3D — pierwszy krok w przepływie pracy skanowania dronem. Użytkownik rysuje poligon na interaktywnej mapie, system waliduje geometrię, konwertuje ją do formatu GeoJSON i wysyła do API .NET 10, które przetwarza żądanie przez wzorzec Mediator (CQRS) i zapisuje definicję obszaru w bazie PostgreSQL z rozszerzeniem PostGIS dla typów danych przestrzennych.

Architektura opiera się na czystej separacji między frontendem Angular 21 (renderowanie mapy, interakcje rysowania, walidacja po stronie klienta) a backendem .NET 10 z MediatR (odbiór API → command/query → handler → persystencja). Obie warstwy wymuszają te same reguły walidacji, aby zapewnić integralność danych.

### Kluczowe decyzje technologiczne

- **Angular 21**: standalone components, signals (nie RxJS dla stanu lokalnego), inject() zamiast constructor injection, nowy control flow (@if/@for), zoneless change detection
- **.NET 10**: primary constructors, file-scoped namespaces, records dla DTO/commands, nullable reference types, global usings
- **MediatR (Mediator Pattern)**: kontroler nie zawiera logiki — wysyła commands/queries do handlerów. Walidacja realizowana przez pipeline behavior (FluentValidation + MediatR pipeline)
- **EF Core 10** z NetTopologySuite i Npgsql dla PostgreSQL + PostGIS
- **Docker Compose**: cały stos uruchamiany przez `docker compose up` — kontener Angular (nginx), kontener .NET API, kontener PostgreSQL 18 z PostGIS. Development z hot-reload przez volume mounts.
- **CI/CD (GitHub Actions)**: pipeline budujący, testujący i publikujący obrazy Docker. Formatowanie kodu weryfikowane na CI.
- **Formatowanie kodu**: backend — JetBrains CleanupCode (ReSharper CLI), frontend — Prettier + ESLint. Oba egzekwowane na CI jako quality gate.
- **OpenAPI (Scalar)**: backend eksponuje specyfikację OpenAPI (`/openapi/v1.json`) z interfejsem Scalar (`/scalar/v1`). Frontend generuje typesafe klienta HTTP za pomocą `openapi-generator-cli` (generator `typescript-angular`). Żadne ręczne pisanie interfejsów HTTP na frontendzie.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   Angular 21 Frontend                        │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │ MapComponent │  │ToolbarComp.  │  │  AreaService     │  │
│  │ (OpenLayers) │  │ (draw/edit/  │  │  (HttpClient)    │  │
│  │  signals     │  │  clear/submit)│  │                  │  │
│  └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘  │
│         │                  │                   │            │
│         ▼                  ▼                   │            │
│  ┌─────────────────────────────────────┐       │            │
│  │      PolygonValidatorService        │       │            │
│  │ (vertex count, closure, intersect,  │       │            │
│  │  area min/max)                      │       │            │
│  └─────────────────────────────────────┘       │            │
│                                                │            │
└────────────────────────────────────────────────┼────────────┘
                                                 │ HTTP POST
                                                 ▼
┌─────────────────────────────────────────────────────────────┐
│                   .NET 10 Backend (MediatR)                  │
│                                                             │
│  ┌──────────────┐        ┌───────────────────────────────┐  │
│  │AreasEndpoint │  send  │  MediatR Pipeline             │  │
│  │ POST /api/   │───────▶│                               │  │
│  │   areas      │        │  ┌─────────────────────────┐  │  │
│  └──────────────┘        │  │ ValidationBehavior      │  │  │
│                          │  │ (FluentValidation)      │  │  │
│                          │  └───────────┬─────────────┘  │  │
│                          │              ▼                 │  │
│                          │  ┌─────────────────────────┐  │  │
│                          │  │ CreateAreaCommandHandler │  │  │
│                          │  │ (business logic)        │  │  │
│                          │  └───────────┬─────────────┘  │  │
│                          └──────────────┼────────────────┘  │
│                                         ▼                   │
│                                ┌──────────────────┐         │
│                                │  AreaRepository  │         │
│                                │  (EF Core 10 +   │         │
│                                │   NTS/PostGIS)   │         │
│                                └────────┬─────────┘         │
│                                         │                   │
└─────────────────────────────────────────┼───────────────────┘
                                          │
                                          ▼
                                 ┌──────────────────┐
                                 │   PostgreSQL     │
                                 │  (PostGIS geom)  │
                                 └──────────────────┘
```

Diagram przedstawia przepływ danych: frontend wysyła HTTP POST → endpoint (minimal API lub slim controller) przekazuje command do MediatR → pipeline behavior waliduje strukturę i geometrię → handler persystuje dane przez repozytorium.

## Components and Interfaces

### Komponenty frontendowe (Angular 21)

#### MapComponent

Główny komponent hostujący mapę OpenLayers. Wykorzystuje signals do zarządzania stanem i inject() do dependency injection.

```typescript
// map.component.ts
@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrl: './map.component.scss',
  standalone: true,
  imports: [MapToolbarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MapComponent implements OnInit, OnDestroy {
  private readonly polygonValidator = inject(PolygonValidatorService);
  private readonly areaService = inject(AreaService);

  private map!: Map;
  private vectorSource = new VectorSource();
  private vectorLayer!: VectorLayer<VectorSource>;
  private drawInteraction: Draw | null = null;
  private modifyInteraction: Modify | null = null;

  // Signals for reactive state
  readonly validationResult = signal<ValidationResult | null>(null);
  readonly isSubmitting = signal(false);
  readonly submissionError = signal<string | null>(null);
  readonly hasPolygon = signal(false);
  readonly isDrawing = signal(false);

  // Computed signals
  readonly isValid = computed(() => this.validationResult()?.isValid ?? false);
  readonly validationErrors = computed(() =>
    this.validationResult()?.errors.map(e => e.message) ?? []
  );

  ngOnInit(): void {
    this.initializeMap();
  }

  private initializeMap(): void {
    this.vectorLayer = new VectorLayer({
      source: this.vectorSource,
      style: this.getDefaultStyle()
    });

    this.map = new Map({
      target: 'map-container',
      layers: [
        new TileLayer({ source: new XYZ({ url: '...' }) }),
        this.vectorLayer
      ],
      view: new View({
        projection: 'EPSG:3857', // Web Mercator for tile compatibility
        center: fromLonLat([20.0, 52.0]),
        zoom: 6
      })
    });
  }

  startDrawing(): void { /* activates Draw interaction, sets isDrawing signal */ }
  clearPolygon(): void { /* removes features, resets all signals */ }
  submitArea(): void { /* validates, transforms coords from EPSG:3857 to EPSG:4326 (toLonLat), converts to GeoJSON, sends to API */ }
}
```

#### MapToolbarComponent

Komponent prezentacyjny z nowymi signal inputs (Angular 21).

```typescript
// map-toolbar.component.ts
@Component({
  selector: 'app-map-toolbar',
  standalone: true,
  templateUrl: './map-toolbar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MapToolbarComponent {
  // Signal inputs (Angular 21)
  readonly isDrawing = input(false);
  readonly hasPolygon = input(false);
  readonly isValid = input(false);
  readonly isSubmitting = input(false);
  readonly validationErrors = input<string[]>([]);

  // Output events
  readonly draw = output<void>();
  readonly clear = output<void>();
  readonly submit = output<void>();
}
```

#### PolygonValidatorService

Czysta logika walidacji wyodrębniona do osobnego serwisu dla testowalności.

```typescript
// polygon-validator.service.ts
export interface ValidationResult {
  isValid: boolean;
  errors: ValidationError[];
}

export interface ValidationError {
  rule: ValidationRule;
  message: string;
}

export enum ValidationRule {
  MinVertices = 'MIN_VERTICES',
  Closure = 'CLOSURE',
  SelfIntersection = 'SELF_INTERSECTION',
  AreaTooLarge = 'AREA_TOO_LARGE',
  AreaTooSmall = 'AREA_TOO_SMALL'
}

@Injectable({ providedIn: 'root' })
export class PolygonValidatorService {
  private readonly MAX_AREA_HECTARES = 5;
  private readonly MIN_AREA_SQM = 100;

  validate(coordinates: number[][]): ValidationResult {
    const errors: ValidationError[] = [];

    if (!this.hasMinVertices(coordinates)) {
      errors.push({ rule: ValidationRule.MinVertices, message: 'Polygon must have at least 3 vertices.' });
    }
    if (!this.isClosed(coordinates)) {
      errors.push({ rule: ValidationRule.Closure, message: 'Polygon must be closed (first and last vertex must be identical).' });
    }
    if (this.hasSelfIntersection(coordinates)) {
      errors.push({ rule: ValidationRule.SelfIntersection, message: 'Polygon edges must not cross each other.' });
    }

    const areaSqm = this.calculateAreaSqm(coordinates);
    if (areaSqm > this.MAX_AREA_HECTARES * 10000) {
      errors.push({ rule: ValidationRule.AreaTooLarge, message: `Polygon area must not exceed ${this.MAX_AREA_HECTARES} hectares.` });
    }
    if (areaSqm < this.MIN_AREA_SQM) {
      errors.push({ rule: ValidationRule.AreaTooSmall, message: `Polygon area must be at least ${this.MIN_AREA_SQM} square meters.` });
    }

    return { isValid: errors.length === 0, errors };
  }

  hasMinVertices(coordinates: number[][]): boolean {
    const distinctCount = this.isClosed(coordinates) ? coordinates.length - 1 : coordinates.length;
    return distinctCount >= 3;
  }

  isClosed(coordinates: number[][]): boolean {
    if (coordinates.length < 2) return false;
    const first = coordinates[0];
    const last = coordinates[coordinates.length - 1];
    return first[0] === last[0] && first[1] === last[1];
  }

  hasSelfIntersection(coordinates: number[][]): boolean {
    // Check if any non-adjacent edges intersect using segment intersection algorithm
  }

  calculateAreaSqm(coordinates: number[][]): number {
    // Spherical excess formula for geographic coordinates
  }
}
```

#### AreaService

Serwis klienta HTTP z inject() (Angular 21). **Generowany automatycznie z OpenAPI spec backendu** za pomocą `openapi-generator` — nie piszemy ręcznie interfejsów HTTP. Backend eksponuje `/swagger/v1/swagger.json`, a frontend generuje z niego typesafe klienta.

```typescript
// Generated by openapi-generator from backend's swagger.json
// src/app/api/services/areas.service.ts (auto-generated)
// Re-exported for app usage:

// area.service.ts — wrapper over generated client (optional, for app-level concerns)
@Injectable({ providedIn: 'root' })
export class AreaService {
  private readonly areasApi = inject(AreasApiService); // generated

  createArea(geojson: CreateAreaRequest): Observable<AreaResponse> {
    return this.areasApi.createArea({ body: geojson });
  }
}
```

**Generowanie klienta (skrypt npm):**
```bash
# frontend/package.json scripts:
# "api:generate": "openapi-generator-cli generate -i http://localhost:5000/openapi/v1.json -g typescript-angular -o src/app/api --additional-properties=useSingleRequestParameter=true"
```

### Komponenty backendowe (.NET 10 + MediatR)

#### AreasEndpoint (Minimal API lub Slim Controller)

Endpoint nie zawiera logiki biznesowej — jedynie przekazuje command do MediatR.

```csharp
// Endpoints/AreasEndpoint.cs
namespace DroneMesh3D.Api.Endpoints;

public static class AreasEndpoint
{
    public static void MapAreasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/areas").WithTags("Areas");

        group.MapPost("/", CreateArea)
            .Produces<AreaResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", GetArea)
            .Produces<AreaResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateArea(
        CreateAreaRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new CreateAreaCommand(request.Type, request.Coordinates);
        var result = await mediator.Send(command, ct);

        return result.Match(
            success => Results.Created($"/api/areas/{success.Id}", success),
            validationFailure => Results.UnprocessableEntity(validationFailure),
            badRequest => Results.BadRequest(badRequest)
        );
    }

    private static async Task<IResult> GetArea(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetAreaQuery(id);
        var result = await mediator.Send(query, ct);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }
}
```

#### Commands i Queries (CQRS z MediatR)

```csharp
// Commands/CreateAreaCommand.cs
namespace DroneMesh3D.Api.Commands;

public record CreateAreaCommand(
    string Type,
    double[][][] Coordinates
) : IRequest<OneOf<AreaResponse, ValidationErrorResponse, ErrorResponse>>;

// Queries/GetAreaQuery.cs
namespace DroneMesh3D.Api.Queries;

public record GetAreaQuery(Guid Id) : IRequest<AreaResponse?>;
```

#### Command Handler

Handler realizuje logikę biznesową: walidacja struktury GeoJSON → walidacja geometrii → konwersja → persystencja.

```csharp
// Handlers/CreateAreaCommandHandler.cs
namespace DroneMesh3D.Api.Handlers;

public sealed class CreateAreaCommandHandler(
    IAreaValidator areaValidator,
    IAreaRepository areaRepository)
    : IRequestHandler<CreateAreaCommand, OneOf<AreaResponse, ValidationErrorResponse, ErrorResponse>>
{
    public async Task<OneOf<AreaResponse, ValidationErrorResponse, ErrorResponse>> Handle(
        CreateAreaCommand command,
        CancellationToken ct)
    {
        // 1. Validate GeoJSON structure
        if (!GeoJsonValidator.IsValidPolygon(command.Type, command.Coordinates))
            return new ErrorResponse("Invalid GeoJSON Polygon geometry.");

        // 2. Reject multi-ring polygons (holes not supported)
        if (command.Coordinates.Length > 1)
            return new ErrorResponse("Multi-ring polygons (holes) are not supported.");

        // 3. Validate geometry rules
        var outerRing = command.Coordinates[0];
        var validationResult = areaValidator.Validate(outerRing);
        if (!validationResult.IsValid)
            return new ValidationErrorResponse(validationResult.Errors);

        // 4. Convert and persist
        var geometry = GeometryConverter.ToPolygon(outerRing);
        var entity = new AreaEntity
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = DateTimeOffset.UtcNow,
            Geometry = geometry
        };

        await areaRepository.AddAsync(entity, ct);

        return new AreaResponse(
            entity.Id,
            entity.CreatedAt,
            new GeoJsonGeometry("Polygon", command.Coordinates));
    }
}
```

#### Query Handler

```csharp
// Handlers/GetAreaQueryHandler.cs
namespace DroneMesh3D.Api.Handlers;

public sealed class GetAreaQueryHandler(IAreaRepository areaRepository)
    : IRequestHandler<GetAreaQuery, AreaResponse?>
{
    public async Task<AreaResponse?> Handle(GetAreaQuery query, CancellationToken ct)
    {
        var entity = await areaRepository.GetByIdAsync(query.Id, ct);
        if (entity is null) return null;

        return new AreaResponse(
            entity.Id,
            entity.CreatedAt,
            GeometryConverter.ToGeoJson(entity.Geometry));
    }
}
```

#### Validation Pipeline Behavior (MediatR)

Automatyczna walidacja w pipeline MediatR za pomocą FluentValidation. 

**Uwaga dotycząca dwóch warstw walidacji:** `ValidationBehavior` (FluentValidation) waliduje strukturę requestu (np. czy pole Type nie jest puste, czy Coordinates nie jest null) — to walidacja formalna DTO. Walidacja geometrii (samoprzecięcia, limity powierzchni) realizowana jest wewnątrz `CreateAreaCommandHandler` przez `IAreaValidator` i zwraca wynik przez `OneOf` zamiast wyjątku. Dzięki temu:
- Walidacja formalna (FluentValidation) → rzuca wyjątek przechwycony przez global exception handler → HTTP 400
- Walidacja biznesowa (IAreaValidator) → zwraca `ValidationErrorResponse` przez OneOf → HTTP 422

```csharp
// Behaviors/ValidationBehavior.cs
namespace DroneMesh3D.Api.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

#### DTO (records w .NET 10)

```csharp
// DTOs/CreateAreaRequest.cs
namespace DroneMesh3D.Api.DTOs;

public record CreateAreaRequest(
    string Type,
    double[][][] Coordinates);

// DTOs/AreaResponse.cs
public record AreaResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    GeoJsonGeometry Geometry);

// DTOs/GeoJsonGeometry.cs
public record GeoJsonGeometry(
    string Type,
    double[][][] Coordinates);

// DTOs/ErrorResponse.cs
public record ErrorResponse(string Message);

// DTOs/ValidationErrorResponse.cs
public record ValidationErrorResponse(List<string> Errors)
{
    public string Message { get; init; } = "Validation failed.";
}
```

#### AreaValidator (backend)

Serwis walidacji po stronie serwera odzwierciedlający reguły frontendowe.

```csharp
// Validation/AreaValidator.cs
namespace DroneMesh3D.Api.Validation;

public sealed class AreaValidator : IAreaValidator
{
    private const double MaxAreaHectares = 5.0;
    private const double MinAreaSqm = 100.0;

    public ValidationResult Validate(double[][] ring)
    {
        var errors = new List<string>();

        if (!HasMinimumVertices(ring))
            errors.Add("Polygon must have at least 3 vertices.");
        if (!IsClosed(ring))
            errors.Add("Polygon must be closed.");
        if (HasSelfIntersection(ring))
            errors.Add("Polygon must not self-intersect.");

        var areaSqm = CalculateAreaSqm(ring);
        if (areaSqm > MaxAreaHectares * 10000)
            errors.Add($"Polygon area exceeds maximum of {MaxAreaHectares} hectares.");
        if (areaSqm < MinAreaSqm)
            errors.Add($"Polygon area is below minimum of {MinAreaSqm} square meters.");

        return new ValidationResult(errors.Count == 0, errors);
    }

    public bool HasMinimumVertices(double[][] ring) =>
        GetDistinctVertexCount(ring) >= 3;

    public bool IsClosed(double[][] ring) =>
        ring.Length >= 2 && ring[0][0] == ring[^1][0] && ring[0][1] == ring[^1][1];

    public bool HasSelfIntersection(double[][] ring)
    {
        // Sweep line or brute-force edge intersection check
    }

    public double CalculateAreaSqm(double[][] ring)
    {
        // Spherical polygon area using geographic coordinates
    }

    private int GetDistinctVertexCount(double[][] ring) =>
        IsClosed(ring) ? ring.Length - 1 : ring.Length;
}
```

#### AreaRepository

Warstwa dostępu do danych z primary constructor (EF Core 10).

```csharp
// Repositories/AreaRepository.cs
namespace DroneMesh3D.Api.Repositories;

public sealed class AreaRepository(AppDbContext context) : IAreaRepository
{
    public async Task AddAsync(AreaEntity entity, CancellationToken ct = default)
    {
        context.Areas.Add(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task<AreaEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Areas.FindAsync([id], ct);
}
```

#### Rejestracja serwisów w Program.cs

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Scalar
builder.Services.AddOpenApi();

// MediatR + pipeline behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Application services
builder.Services.AddScoped<IAreaValidator, AreaValidator>();
builder.Services.AddScoped<IAreaRepository, AreaRepository>();

// EF Core with spatial types (PostgreSQL + PostGIS)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        x => x.UseNetTopologySuite()));

// CORS for Angular dev
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapAreasEndpoints();
app.Run();
```

## Data Models

### Schemat bazy danych

```sql
-- Wymaga rozszerzenia PostGIS
CREATE EXTENSION IF NOT EXISTS postgis;

CREATE TABLE "Areas" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT now(),
    "Geometry" GEOMETRY(Polygon, 4326) NOT NULL
);

CREATE INDEX IX_Areas_Geometry ON "Areas" USING GIST ("Geometry");
```

### Encja Entity Framework

```csharp
// Entities/AreaEntity.cs
namespace DroneMesh3D.Api.Entities;

using NetTopologySuite.Geometries;

public sealed class AreaEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required Geometry Geometry { get; set; }
}
```

### Konfiguracja EF Core

```csharp
// Data/AppDbContext.cs
namespace DroneMesh3D.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AreaEntity> Areas => Set<AreaEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<AreaEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Geometry).HasColumnType("geometry(Polygon, 4326)");
        });
    }
}
```

### Interfejsy frontendowe

```typescript
// models/geojson.ts
export interface GeoJsonPolygon {
  type: 'Polygon';
  coordinates: number[][][]; // [ring[vertex[lng, lat]]]
}

// models/validation.ts
export interface ValidationResult {
  isValid: boolean;
  errors: ValidationError[];
}

export interface ValidationError {
  rule: ValidationRule;
  message: string;
}

export enum ValidationRule {
  MinVertices = 'MIN_VERTICES',
  Closure = 'CLOSURE',
  SelfIntersection = 'SELF_INTERSECTION',
  AreaTooLarge = 'AREA_TOO_LARGE',
  AreaTooSmall = 'AREA_TOO_SMALL'
}
```

### Interfejsy backendowe

```csharp
// Interfaces/IAreaValidator.cs
namespace DroneMesh3D.Api.Interfaces;

public interface IAreaValidator
{
    ValidationResult Validate(double[][] ring);
    bool HasMinimumVertices(double[][] ring);
    bool IsClosed(double[][] ring);
    bool HasSelfIntersection(double[][] ring);
    double CalculateAreaSqm(double[][] ring);
}

// Interfaces/IAreaRepository.cs
public interface IAreaRepository
{
    Task AddAsync(AreaEntity entity, CancellationToken ct = default);
    Task<AreaEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
```

### Kontrakt API

#### POST /api/areas

Tworzy nową definicję obszaru z poligonu GeoJSON.

**Żądanie:**

```http
POST /api/areas
Content-Type: application/json

{
  "type": "Polygon",
  "coordinates": [
    [
      [21.0122, 52.2297],
      [21.0130, 52.2297],
      [21.0130, 52.2290],
      [21.0122, 52.2290],
      [21.0122, 52.2297]
    ]
  ]
}
```

**Odpowiedź sukcesu (201 Created):**

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "createdAt": "2026-06-04T10:30:00Z",
  "geometry": {
    "type": "Polygon",
    "coordinates": [
      [
        [21.0122, 52.2297],
        [21.0130, 52.2297],
        [21.0130, 52.2290],
        [21.0122, 52.2290],
        [21.0122, 52.2297]
      ]
    ]
  }
}
```

**Odpowiedź błędu (400 Bad Request):**

```json
{
  "message": "Invalid GeoJSON Polygon geometry."
}
```

**Odpowiedź błędu (422 Unprocessable Entity):**

```json
{
  "message": "Validation failed.",
  "errors": [
    "Polygon must not self-intersect.",
    "Polygon area exceeds maximum of 5 hectares."
  ]
}
```

**Odpowiedź błędu (500 Internal Server Error):**

```json
{
  "message": "An unexpected error occurred."
}
```

## Infrastruktura Docker

Cały system uruchamiany jest przez Docker Compose. Trzy kontenery:

### docker-compose.yml

```yaml
# docker-compose.yml
services:
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "4200:80"
    depends_on:
      - api
    networks:
      - dronemesh

  api:
    build:
      context: ./backend
      dockerfile: Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Default=Host=db;Database=dronemesh3d;Username=postgres;Password=YourStr0ngP@ssword
    depends_on:
      db:
        condition: service_healthy
    networks:
      - dronemesh

  db:
    image: postgis/postgis:18-3.5
    environment:
      - POSTGRES_DB=dronemesh3d
      - POSTGRES_PASSWORD=YourStr0ngP@ssword
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: pg_isready -U postgres
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - dronemesh

volumes:
  pgdata:

networks:
  dronemesh:
    driver: bridge
```

### Dockerfile — Backend (.NET 10)

```dockerfile
# backend/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DroneMesh3D.Api.dll"]
```

### Dockerfile — Frontend (Angular 21 + nginx)

```dockerfile
# frontend/Dockerfile
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json .
RUN npm ci
COPY . .
RUN npm run build -- --configuration production

FROM nginx:alpine AS runtime
COPY --from=build /app/dist/dronemesh3d-frontend/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

### nginx.conf (reverse proxy do API)

```nginx
# frontend/nginx.conf
server {
    listen 80;
    server_name localhost;

    location / {
        root /usr/share/nginx/html;
        index index.html;
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://api:8080/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### docker-compose.override.yml (development z hot-reload)

```yaml
# docker-compose.override.yml
services:
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile.dev
    ports:
      - "4200:4200"
    volumes:
      - ./frontend:/app
      - /app/node_modules
    command: npm start -- --host 0.0.0.0

  api:
    build:
      context: ./backend
      dockerfile: Dockerfile
      target: build
    ports:
      - "5000:8080"
    volumes:
      - ./backend:/src
    command: dotnet watch run --urls http://0.0.0.0:8080
    environment:
      - DOTNET_USE_POLLING_FILE_WATCHER=1
```

Przy developmencie wystarczy `docker compose up` — frontend z hot-reload, API z `dotnet watch`, SQL Server gotowy. W produkcji budujemy multi-stage images (zoptymalizowane, bez SDK).

## CI/CD (GitHub Actions)

Pipeline uruchamiany przy każdym pushu i pull requeście. Składa się z trzech jobów: formatowanie, testy, build obrazów Docker.

### Workflow: ci.yml

```yaml
# .github/workflows/ci.yml
name: CI

on:
  pull_request:
    branches: [main]

jobs:
  format-check:
    name: Code Formatting
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      # Backend - JetBrains CleanupCode
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install JetBrains CleanupCode
        run: dotnet tool install -g JetBrains.ReSharper.GlobalTools

      - name: Run CleanupCode (dry-run)
        run: |
          jb cleanupcode backend/DroneMesh3D.Api.sln --profile="Built-in: Reformat Code" --no-build
          git diff --exit-code -- backend/ || (echo "::error::Backend code is not formatted. Run 'jb cleanupcode' locally." && exit 1)

      # Frontend - Prettier + ESLint
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: 22
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json

      - name: Install frontend deps
        run: npm ci
        working-directory: frontend

      - name: Check Prettier formatting
        run: npx prettier --check .
        working-directory: frontend

      - name: Run ESLint
        run: npx eslint . --max-warnings=0
        working-directory: frontend

  test:
    name: Tests
    runs-on: ubuntu-latest
    needs: format-check
    services:
      postgres:
        image: postgis/postgis:18-3.5
        env:
          POSTGRES_DB: dronemesh3d_test
          POSTGRES_PASSWORD: YourStr0ngP@ssword
        ports:
          - 5432:5432
        options: >-
          --health-cmd "pg_isready -U postgres"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4

      # Backend tests
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore & Build backend
        run: dotnet build --configuration Release
        working-directory: backend

      - name: Run backend tests
        run: dotnet test --configuration Release --no-build --logger trx
        working-directory: backend
        env:
          ConnectionStrings__Default: "Host=localhost;Database=dronemesh3d_test;Username=postgres;Password=YourStr0ngP@ssword"

      # Frontend tests
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: 22
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json

      - name: Install & Test frontend
        run: |
          npm ci
          npm run test -- --watch=false --browsers=ChromeHeadless
        working-directory: frontend

  build:
    name: Build Docker Images
    runs-on: ubuntu-latest
    needs: test
    steps:
      - uses: actions/checkout@v4

      - name: Build API image
        run: docker build -t dronemesh3d-api:${{ github.sha }} ./backend

      - name: Build Frontend image
        run: docker build -t dronemesh3d-frontend:${{ github.sha }} ./frontend
```

### Formatowanie kodu — konfiguracja lokalna

**Backend (.NET — CleanupCode):**

```xml
<!-- backend/.editorconfig -->
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
dotnet_sort_system_directives_first = true
csharp_style_namespace_declarations = file_scoped
csharp_style_prefer_primary_constructors = true
```

Uruchomienie lokalne:
```bash
dotnet tool install -g JetBrains.ReSharper.GlobalTools
jb cleanupcode backend/DroneMesh3D.Api.sln --profile="Built-in: Reformat Code"
```

**Frontend (Prettier + ESLint):**

```json
// frontend/.prettierrc
{
  "semi": true,
  "singleQuote": true,
  "trailingComma": "es5",
  "printWidth": 100,
  "tabWidth": 2
}
```

Uruchomienie lokalne:
```bash
npx prettier --write .
npx eslint . --fix
```

## Error Handling

| Warstwa | Warunek błędu | Strategia obsługi |
|---------|---------------|-------------------|
| Frontend | Poligon nie przeszedł walidacji | Wyświetlenie konkretnego komunikatu błędu, podświetlenie poligonu na czerwono, dezaktywacja przycisku wysyłania |
| Frontend | Błąd sieciowy przy wysyłaniu | Wyświetlenie komunikatu błędu (toast), ponowne aktywowanie przycisku wysyłania do ponawiania |
| Backend | Nieprawidłowa struktura GeoJSON | Handler zwraca ErrorResponse → endpoint mapuje na HTTP 400 |
| Backend | Niepowodzenie walidacji geometrii | Handler zwraca ValidationErrorResponse → endpoint mapuje na HTTP 422 |
| Backend | Błąd zapisu do bazy danych | Global exception handler → HTTP 500 z ogólnym komunikatem, logowanie szczegółów wewnętrznie |
| Backend | Nieoczekiwany wyjątek | Global exception handler → HTTP 500, brak ujawniania śladów stosu |

## Testing Strategy

### Testy jednostkowe (oparte na przykładach)

- **MapComponent**: Weryfikacja inicjalizacji mapy (warstwa satelitarna, projekcja EPSG:4326), cykl życia interakcji Draw/Modify, stany signals, stany UI ładowania/błędu.
- **MapToolbarComponent**: Weryfikacja stanów enabled/disabled przycisków na podstawie signal inputs, emisji zdarzeń przy kliknięciu.
- **AreaService**: Weryfikacja wywołania HTTP POST z prawidłowym URL i ładunkiem (provideHttpClientTesting).
- **CreateAreaCommandHandler**: Weryfikacja prawidłowych odpowiedzi dla każdego scenariusza (sukces, błąd walidacji, nieprawidłowy GeoJSON) z reprezentatywnymi przykładami.
- **AreaRepository**: Testy integracyjne z in-memory provider lub testcontainers SQL Server.

### Testy oparte na właściwościach (Property-Based Tests)

Testy oparte na właściwościach walidują uniwersalne niezmienniki na losowo generowanych danych wejściowych (minimum 100 iteracji na właściwość). Celują w czystą logikę walidacji i konwersji:

- **PolygonValidatorService** (frontend): Właściwości 1–4 testujące liczbę wierzchołków, zamknięcie, samoprzecięcia i limity powierzchni.
- **Konwersja GeoJSON**: Właściwość 5 testująca zachowanie współrzędnych przez pipeline konwersji.
- **AreaValidator** (backend): Właściwości 6–7 testujące walidację struktury GeoJSON i zgodność reguł z frontendem.
- **Round-trip persystencji**: Właściwość 8 testująca, że zapisane obszary mogą być pobrane z identyczną geometrią.

### Testy integracyjne

- POST /api/areas end-to-end z prawidłowym poligonem → weryfikacja 201 i rekordu w bazie (WebApplicationFactory).
- POST /api/areas z nieprawidłowym ciałem żądania → weryfikacja 400.
- POST /api/areas z nieprawidłową geometrią → weryfikacja 422.
- Symulacja awarii bazy danych → weryfikacja 500 bez ujawniania szczegółów wewnętrznych.

## Correctness Properties

*Właściwość (property) to cecha lub zachowanie, które powinno być prawdziwe we wszystkich poprawnych wykonaniach systemu — formalnie: stwierdzenie o tym, co system powinien robić. Właściwości stanowią pomost między specyfikacjami czytelnymi dla człowieka a gwarancjami poprawności weryfikowalnymi maszynowo.*

### Property 1: Walidacja liczby wierzchołków

*Dla dowolnej* tablicy współrzędnych z mniej niż 3 różnymi wierzchołkami (bez wierzchołka zamykającego), PolygonValidator POWINIEN odrzucić ją z błędem MIN_VERTICES; oraz *dla dowolnej* tablicy współrzędnych z 3 lub więcej różnymi wierzchołkami, walidator NIE POWINIEN produkować błędu MIN_VERTICES.

**Validates: Requirements 3.1**

### Property 2: Walidacja zamknięcia poligonu

*Dla dowolnej* tablicy współrzędnych, w której pierwsza i ostatnia współrzędna nie są identyczne, PolygonValidator POWINIEN odrzucić ją z błędem CLOSURE; oraz *dla dowolnej* tablicy współrzędnych, w której pierwsza i ostatnia współrzędna są identyczne, walidator NIE POWINIEN produkować błędu CLOSURE.

**Validates: Requirements 3.2**

### Property 3: Wykrywanie samoprzecięć

*Dla dowolnego* poligonu, którego krawędzie przecinają się wzajemnie (samoprzecinający się), PolygonValidator POWINIEN odrzucić go z błędem SELF_INTERSECTION; oraz *dla dowolnego* prostego (niesamoprzecinającego się) poligonu, walidator NIE POWINIEN produkować błędu SELF_INTERSECTION.

**Validates: Requirements 3.3**

### Property 4: Walidacja limitów powierzchni

*Dla dowolnego* prawidłowego poligonu: jeśli jego obliczona powierzchnia przekracza 5 hektarów (50 000 m²), PolygonValidator POWINIEN odrzucić go z błędem AREA_TOO_LARGE; jeśli jego obliczona powierzchnia jest poniżej 100 m², PolygonValidator POWINIEN odrzucić go z błędem AREA_TOO_SMALL; a jeśli powierzchnia mieści się w przedziale [100 m², 50 000 m²], żaden błąd dotyczący powierzchni NIE POWINIEN być produkowany.

**Validates: Requirements 3.4, 3.5**

### Property 5: Zachowanie współrzędnych w GeoJSON

*Dla dowolnej* prawidłowej tablicy współrzędnych poligonu, konwersja do obiektu geometrii GeoJSON Polygon POWINNA wyprodukować wynik, w którym każda współrzędna wejściowa występuje w tablicy coordinates jako para [longitude, latitude], w oryginalnej kolejności, bez dodawania ani usuwania współrzędnych.

**Validates: Requirements 4.2, 4.3**

### Property 6: Backend odrzuca nieprawidłowy GeoJSON

*Dla dowolnego* ciała żądania JSON, które nie jest zgodne ze strukturą GeoJSON Polygon (brakujące pole type, nieprawidłowa wartość type, brakujące coordinates, nienumeryczne współrzędne, pusty pierścień), Area_API POWINIEN zwrócić odpowiedź HTTP 400.

**Validates: Requirements 5.2, 5.3**

### Property 7: Zgodność walidacji frontend-backend

*Dla dowolnej* tablicy współrzędnych poligonu, backendowy AreaValidator POWINIEN produkować ten sam werdykt walidacji (pass/fail) i identyfikować te same naruszone reguły co frontendowy PolygonValidatorService.

**Validates: Requirements 5.4, 5.5**

### Property 8: Round-trip persystencji obszaru

*Dla dowolnej* prawidłowej definicji obszaru, która została pomyślnie wysłana, pobranie obszaru po jego zwróconym identyfikatorze POWINNO dać rekord zawierający te same współrzędne geometrii poligonu, niepusty unikalny identyfikator oraz prawidłowy znacznik czasu utworzenia.

**Validates: Requirements 6.1, 6.3**
