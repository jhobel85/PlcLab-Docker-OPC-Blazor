# Changelog

All notable changes to this project will be documented here.

## [v0.2.0] - 2026-04-04

### Added
- Swagger UI and OpenAPI generation for all API endpoints
- Bearer JWT security scheme in Swagger with operation-level security requirements
- Swagger endpoint metadata (tags, summaries, descriptions, response types) across certificates, seed, test plans, test runs, and health endpoints
- Dev setup documentation for Swagger URLs and authorization usage
- Automated Swagger/OpenAPI test suite (`SwaggerApiTests`) validating UI availability, JSON generation, endpoint coverage, and security scheme output

### Changed
- `/healthz` moved to a minimal API route backed by `HealthCheckService` so it can be included in OpenAPI docs
- Authentication and authorization middleware enabled in app pipeline

### Fixed
- Explicit `[FromServices]` / `[FromBody]` bindings for minimal API endpoints to avoid inferred-body startup failures during OpenAPI generation

## [v0.1.0] - Initial scaffold
- Blazor Web App skeleton
- OPC UA client factory
- Basic ops documentation
