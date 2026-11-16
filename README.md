# Bicycle Racing

ASP.NET Core MVC website using .NET 10, Identity, EF Core, and SQL Server.

## Project map

| Project | Where to look |
| --- | --- |
| `BicycleRacing.Api` | `Program.cs` for startup; `Controllers`, `Views`, and `Areas/Identity` for pages and accounts; `wwwroot` for static assets. |
| `BicycleRacing.Database` | `Entities` and `Enums` for models; `ApplicationDbContext.cs` for relationships; `Migrations` for schema changes. |
| `BicycleRacing.Services` | Business logic |
| `BicycleRacing.Tests` | Account flow and database relationship tests using SQLite. |

## Start locally

Requires the **.NET 10 SDK** and **SQL Server** (Windows LocalDB by default). Run commands from the repository root.

The connection is configured in `BicycleRacing.Api/appsettings.json`. To use another SQL Server instance:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your SQL Server connection string>" --project BicycleRacing.Api
```

First-time setup:

```powershell
dotnet restore BicycleRacing.slnx
dotnet tool install --global dotnet-ef --version 10.0.12
dotnet dev-certs https --trust
dotnet ef database update --project BicycleRacing.Database --startup-project BicycleRacing.Api
```

If `dotnet-ef` is already installed, ensure it uses version 10.x. Apply `database update` again after pulling new migrations.

Start the website:

```powershell
dotnet run --project BicycleRacing.Api --launch-profile https
```

Keep the terminal running and open [https://localhost:7166/enter-account](https://localhost:7166/enter-account) to register or log in. Signup requires email confirmation; without an email sender, the development confirmation page provides a confirmation link. Stop the app with Ctrl+C before starting another instance.

## Development commands

```powershell
dotnet test BicycleRacing.slnx
dotnet ef migrations add MigrationName --project BicycleRacing.Database --startup-project BicycleRacing.Api --output-dir Migrations
```
