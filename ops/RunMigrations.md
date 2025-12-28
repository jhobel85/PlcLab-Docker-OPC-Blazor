# Run EF Core Migrations (optional)

```bash
# Add migration
 dotnet ef migrations add Init --project src/PlcLab.Infrastructure --startup-project src/PlcLab.Web
# Update DB
 dotnet ef database update --project src/PlcLab.Infrastructure --startup-project src/PlcLab.Web
```

> Ensure connection string is set in `appsettings.json` or env vars.
