# Deploy to Azure (optional)

## Azure App Service (container)
1. Build and push image to registry.
2. Create Web App for Containers.
3. Configure env vars: `ASPNETCORE_URLS`, `OpcUa__Endpoint`.

## Azure Web App (code-based)
1. `dotnet publish` the web project.
2. Deploy with `az webapp deploy` or GitHub Actions.
