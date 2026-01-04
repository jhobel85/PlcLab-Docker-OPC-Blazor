# Run EF Core Migrations

> **Note**: The `InitialCreate` migration has already been created. Only run these commands if you need to create new migrations or update the database.

## Prerequisites
- Ensure .NET 8 SDK is installed (see `global.json` for the required version)
- Ensure `dotnet-ef` v9.0.0 is installed: `dotnet tool install --global dotnet-ef --version 9.0.0`
- Connection string must be set in `appsettings.json` or environment variables

## Create a New Migration

```bash
dotnet ef migrations add <MigrationName> --project src/PlcLab.Infrastructure --startup-project src/PlcLab.Web

dotnet build ; dotnet ef migrations add InitialCreate --project src/PlcLab.Infrastructure --startup-project src/PlcLab.Web
```

## Apply Migrations to Database

```bash
# From the project root directory:
dotnet ef database update --project src/PlcLab.Infrastructure --startup-project src/PlcLab.Web

# Or from PlcLab.Web directory:
cd src/PlcLab.Web
dotnet ef database update --project ../PlcLab.Infrastructure
```

## Troubleshooting

- If you get a "System.Runtime" version error, ensure you have the correct `dotnet-ef` version installed for your target framework
- For .NET 8 projects, use `dotnet-ef` v9.0.0
- For .NET 9 projects, use `dotnet-ef` v10.0.0
