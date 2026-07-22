# Challenge — Users & Transactions API

ASP.NET Core Web API for users and transactions on SQL Server, with CRUD endpoints and
in-memory summary calculations. Built with Clean Architecture, EF Core, AutoMapper and Swagger.

## Architecture

| Project | Responsibility |
| --- | --- |
| `Challenge.Core` | Entities, enums, DTOs, service logic, `IRepository<T>` contract, AutoMapper profile. |
| `Challenge.Infrastructure` | EF Core `DbContext`, configurations, migrations, `Repository<T>`. |
| `Challenge.Api` | Controllers, DI wiring, Swagger, global error handling. |
| `Challenge.UnitTests` | xUnit + NSubstitute service tests. |
| `Challenge.IntegrationTests` | End-to-end tests via `WebApplicationFactory` + Testcontainers. |

Dependency direction: `Api → Core`, `Api → Infrastructure`, `Infrastructure → Core`.
Migrations apply automatically on startup (opt out with `ApplyMigrationsOnStartup=false`).

## Tech stack

- .NET 10
- ASP.NET Core + Swagger
- EF Core 10 (SQL Server)
- AutoMapper
- xUnit
- NSubstitute
- Testcontainers

## Run with Docker Compose

```bash
cp .env.example .env    # change password if needed
docker compose up --build
```

- Swagger: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health`

## Run the API locally

```bash
docker compose up -d sqlserver          # database only
dotnet run --project src/Challenge.Api  # Swagger at http://localhost:5019/swagger
```

The connection string is not committed. Supply it via environment variable or user-secrets:

```bash
dotnet user-secrets --project src/Challenge.Api set \
  "ConnectionStrings:Default" "Server=localhost,1433;Database=ChallengeDb;User Id=sa;Password=<your-password>;TrustServerCertificate=True;"
```

In the container it is provided via `ConnectionStrings__Default`. Swagger is only enabled in
the `Development` environment.

## API endpoints

### Users

| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/users` | List users |
| GET | `/api/users/{id}` | Get user by id |
| POST | `/api/users` | Create user |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Delete user (blocked with `409` while the user still has transactions) |

### Transactions

| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/transactions` | Add a transaction |
| GET | `/api/transactions` | List transactions (optional `?userId=`) |
| GET | `/api/transactions/totals/per-user` | Total amount per user (users with no transactions report `0`) |
| GET | `/api/transactions/totals/per-type` | Total amount per type |
| GET | `/api/transactions/high-volume?threshold={value}` | Amounts `>=` threshold, descending |

`TransactionType`: `0 = Debit`, `1 = Credit`. Invalid input — including a transaction that
references a non-existent user — returns `400`; unique-constraint and foreign-key conflicts
(e.g. deleting a user that still has transactions) return `409`; both as RFC 7807
`ProblemDetails`. Sample requests in `src/Challenge.Api/Challenge.Api.http`.

## Tests

```bash
dotnet test
```

Unit tests need no dependencies. Integration tests require Docker (disposable SQL container
via Testcontainers).

> **Apple Silicon:** compose and the integration tests use `azure-sql-edge` (runs natively on
> arm64). On amd64 you can switch to `mcr.microsoft.com/mssql/server:2022-latest`. If your Docker
> Engine is older than the negotiated API version, export `DOCKER_API_VERSION` (e.g. `1.43`).

## Technical choices

- **Clean Architecture (Core / Infrastructure / Api).** Keeps business logic free of framework
  and persistence concerns, so services are unit-testable without a database and the storage
  technology can change without touching the domain.
- **Generic `IRepository<T>` + EF Core.** A thin abstraction over `DbSet<T>` keeps the services
  persistence-agnostic and easy to mock, without hand-writing a repository per entity.
- **DTOs + AutoMapper.** Entities never cross the HTTP boundary; request/response contracts are
  explicit records with validation attributes, decoupling the API surface from the schema.
- **Summaries computed in the service layer.** The aggregation endpoints
  (`totals/per-user`, `totals/per-type`) are expressed as straightforward LINQ, favouring
  readability for the dataset size this challenge targets.
- **RFC 7807 `ProblemDetails` for errors.** A single `GlobalExceptionHandler` maps database
  constraint violations to meaningful status codes — unique index (`2601/2627`) and foreign-key
  (`547`) conflicts become `409`, everything else a logged `500` — so error handling lives in
  one place instead of every controller.
- **Secrets kept out of source.** The connection string is supplied via environment variables /
  user-secrets and `docker compose` reads the SA password from `.env`. Nothing sensitive is
  committed (`.env.example` documents the shape).
- **Testing pyramid.** Fast unit tests (xUnit + NSubstitute, EF InMemory) cover service and
  controller logic; integration tests (`WebApplicationFactory` + Testcontainers) exercise the
  full pipeline against a real SQL Server, which is the only place constraint behaviour (unique
  index, FK restrict) can be verified.

## Assumptions & trade-offs

- **Deleting a user is blocked (not cascaded) when transactions exist.** Financial history is
  treated as the record of truth, so the foreign key uses `Restrict` and the API returns `409`
  rather than silently deleting transactions. Removing a user with history is therefore a
  deliberate, multi-step operation.
- **A pre-check guards transaction creation.** `AddAsync` verifies the user exists and returns
  `400` before hitting the database. This costs an extra round-trip and has a small TOCTOU
  window, but it cleanly separates "unknown user" (`400`) from a genuine data conflict (`409`),
  which the `Restrict` foreign key would otherwise make ambiguous.
- **Summaries load data into memory rather than paging.** For this challenge's scale that keeps
  the code simple; at production volumes these endpoints would need pagination and/or
  database-side aggregation, and the plain indexes on `Amount` and `UserId` would be revisited.
- **`User.Id` is a client-visible string (GUID).** Simple and safe to expose; a sequential key
  would be smaller but leak ordering/volume.
- **Migrations run on startup by default.** Convenient for local/dev and the container, but
  gated behind `ApplyMigrationsOnStartup` so production can run migrations as a separate,
  controlled step and avoid replicas racing to migrate.
- **Swagger is enabled only in `Development`.** Avoids exposing the API surface by default in
  production; enable it explicitly per environment if needed.
- **Local SQL uses the `sa` account.** Acceptable for a disposable dev container; production is
  expected to supply a least-privilege login through the externalised connection string.
- **`TransactionType` is stored as an `int`.** Compact and index-friendly; adding values is safe
  as long as existing ordinals are preserved.

## Potential improvements

Given more time, the following would be the natural next steps:

- **Pagination & server-side aggregation.** Move the list and `totals/*` endpoints to paged
  queries and database-side `GROUP BY` so they scale beyond in-memory processing.
- **Authentication & authorization.** No auth today — add JWT/OAuth and per-endpoint policies
  before this is exposed to real users.
- **Least-privilege database account by default.** Replace `sa` with a scoped login (and drop
  `TrustServerCertificate`) in the compose setup, not just in production guidance.
- **Structured logging & observability.** Add correlation IDs, OpenTelemetry traces/metrics, and
  ship logs somewhere queryable; the current setup only logs unhandled errors.
- **API versioning & pagination metadata.** Introduce `api/v1` routing and standard paging
  headers/response envelopes for forward compatibility.
- **CI pipeline.** Automate build, analyzers, unit + integration tests, and coverage reporting on
  every push.


