# Bicycle Racing

An ASP.NET Core bicycle racing website targeting .NET 10.

## Solution structure

| Project | Responsibility |
| --- | --- |
| `BicycleRacing.Api` | Application startup, HTTP endpoints, existing MVC views and Identity account pages, and runtime configuration. |
| `BicycleRacing.Database` | EF Core context, SQL Server registration, database entities, and migrations. |
| `BicycleRacing.Services` | Business services and application rules. References the database project; there are no business services in the starter application yet. |

Project references flow from Api to Services and Database, and from Services to Database. Database does not reference either of the other projects. Keep HTTP concerns in Api and put future business logic in Services.

The Api project is the startup project and continues to serve the existing website. Account pages use ASP.NET Core Identity with the context in Database.

## Initial database model

- `User` inherits account fields such as `UserName`, `Email`, and `PasswordHash` from `IdentityUser`, and has collections of participating races and scores. Passwords are managed by Identity rather than stored as plain text.
- `Race` has a required name (up to 200 characters), a start date, an optional end date, participants, and scores. Dates use `DateTimeOffset` to preserve their UTC offsets.
- `Score` links a user and race with their placement and elapsed `TimeSpan`. Placements must be positive and times cannot be negative. Elapsed time is stored as ticks in a SQL Server `bigint`, allowing races longer than 24 hours.

Users and races have a many-to-many relationship through `RaceParticipants`, independent of scores. Each user can have at most one score per race. The context exposes `Users`, `Races`, and `Scores`. Deleting a race removes its scores and participation links; deleting a user with scores is restricted so results are not silently lost.

Validation attributes supply rules for future forms and request models. Business rules such as an end date following its start date or a scored user being a participant belong in the service layer when those workflows are implemented. Database foreign keys and unique indexes enforce the configured relationships and uniqueness.

## Local development

Install the .NET 10 SDK and SQL Server. The default development connection uses SQL Server LocalDB on Windows. Run these commands from the solution directory:

```powershell
dotnet restore BicycleRacing.slnx
dotnet build BicycleRacing.slnx
dotnet run --project BicycleRacing.Api --launch-profile https
```

The HTTPS profile serves the site at `https://localhost:7166` and `http://localhost:5172`. Trust the local HTTPS certificate with `dotnet dev-certs https --trust` if necessary.

To use a different database, set `ConnectionStrings:DefaultConnection` with user secrets on the startup project:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your SQL Server connection string>" --project BicycleRacing.Api
```

The existing user secrets ID and development database name are preserved so the project split continues to use the same local configuration and data.

## Database migrations

Install the EF Core 10 CLI tool if it is not already available:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
```

The database project contains the context and migrations; the API supplies startup configuration. Specify both projects when using EF tools:

```powershell
dotnet ef migrations list --project BicycleRacing.Database --startup-project BicycleRacing.Api
dotnet ef database update --project BicycleRacing.Database --startup-project BicycleRacing.Api
dotnet ef migrations add MigrationName --project BicycleRacing.Database --startup-project BicycleRacing.Api --output-dir Migrations
```

The original Identity migration retains its migration ID. Moving it to the database project does not require recreating an existing database.

## Docker

Build from the solution directory so Docker can access all three projects:

```powershell
docker build -f BicycleRacing.Api/Dockerfile -t bicycleracing .
```

Supply a reachable SQL Server connection using the `ConnectionStrings__DefaultConnection` environment variable when running the container. Windows LocalDB is not available inside the Linux container.
