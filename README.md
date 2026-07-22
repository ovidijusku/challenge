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
Migrations apply automatically on startup.

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
docker compose up --build
```

- Swagger: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health`

## Run the API locally

```bash
docker compose up -d sqlserver          # database only
dotnet run --project src/Challenge.Api  # Swagger at http://localhost:5019/swagger
```

Connection string: `src/Challenge.Api/appsettings.json` (`ConnectionStrings:Default`);
overridden in the container via `ConnectionStrings__Default`.

## API endpoints

### Users

| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/users` | List users |
| GET | `/api/users/{id}` | Get user by id |
| POST | `/api/users` | Create user |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Delete user |

### Transactions

| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/transactions` | Add a transaction |
| GET | `/api/transactions` | List transactions (optional `?userId=`) |
| GET | `/api/transactions/totals/per-user` | Total amount per user (users with no transactions report `0`) |
| GET | `/api/transactions/totals/per-type` | Total amount per type |
| GET | `/api/transactions/high-volume?threshold={value}` | Amounts `>=` threshold, descending |

`TransactionType`: `0 = Debit`, `1 = Credit`. Invalid input — including a transaction that
references a non-existent user — returns `400`; unique-constraint violations return `409`;
both as RFC 7807 `ProblemDetails`. Sample requests in
`src/Challenge.Api/Challenge.Api.http`.

## Tests

```bash
dotnet test
```

Unit tests need no dependencies. Integration tests require Docker (disposable SQL container
via Testcontainers).

> **Apple Silicon:** compose and the integration tests use `azure-sql-edge` (runs natively on
> arm64). On amd64 you can switch to `mcr.microsoft.com/mssql/server:2022-latest`. If your Docker
> Engine is older than the negotiated API version, export `DOCKER_API_VERSION` (e.g. `1.43`).
