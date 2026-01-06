# PlcLab-Docker-OPC-Blazor task list and documentation

A focused task list to build a Blazor web app that uses the **OPC Foundation UA .NET Standard** stack and a **virtual OPC UA server** (Reference Server) for demo, testing, and CI.

## Virtual PLC
Add the official OPC UA **Reference Server** as a Docker service (port **4840**) and set `OpcUa:Endpoint` accordingly.

> Endpoints supported:
> - Dockerized **OPC UA Reference Server** (virtual PLC)
> - In‑process mock OPC UA server (optional)

---

## 1) Repository & Projects
- [x] Create solution `PlcLab-Docker-OPC-Blazor` with projects:
  - [x] `PlcLab.Web` (Blazor Web App; SSR + interactive)
  - [x] `PlcLab.OPC` (OPC UA client utils; connect/browse/subscribe/methods)
  - [x] `PlcLab.Domain` (entities, value objects, domain events)
  - [x] `PlcLab.Application` (CQRS + test runner)
  - [x] `PlcLab.Infrastructure` (EF Core, Serilog, OpenTelemetry)
- [x] Add `.editorconfig`, `.gitattributes`, `.gitignore`
- [x] Use **Directory.Packages.props** to pin package versions (OPC UA only)

## 2) Virtual PLC (OPC UA Reference Server) via Docker
- [x] Add `opcua-refserver` service to `docker-compose.yml` (port 4840)
- [x] Configure `OpcUa:Endpoint=opc.tcp://opcua-refserver:4840` in `appsettings.json`
- [x] Document trust list/certificate steps for the client
- [x] Create a hosted service in `PlcLab.Infrastructure` that seeds demo data on startup (guard with config flag `Seed:Enabled`). Seed demo nodes/methods:
  - [x] Variables: `Process/State`, `Analog/Flow`, `Digital/ValveOpen`
  - [x] Methods: `Add`, `ResetAlarms`

## 3) OPC UA Client (PlcLab.OPC)
- [x] Implement `OpcUaClientFactory.ConnectAsync` (cert management, secure policies)
- [x] Browsing helpers: path → `NodeId`, recursive tree build
- [x] Subscriptions: create/update/dispose; callbacks for values
- [x] Read/Write helpers: scalars, arrays, variants
- [x] Method invocation: objectId + methodId + input arguments; result parsing

## 4) Domain & Application
- [x] Define entities: `TestPlan`, `TestCase`, `TestRun`, `TestResult`, `SignalSnapshot`
- [x] Domain events: `TestCaseStarted`, `TestCasePassed`, `TestCaseFailed`
- [x] Orchestrate test runs (connect, execute cases, capture results, teardown)
- [x] Validation rules (limits, timeouts, required signals)

## 5) Persistence & Observability
- [x] Choose DB (PostgreSQL or SQL Server) — **optional** for demo
- [x] EF Core DbContext + migrations
- [x] Serilog: console + rolling file
- [x] OpenTelemetry: traces for connect/read/write/method

## 6) Blazor UI
- [X] **Endpoint Switcher** (Virtual PLC / In‑process mock)
- [x] **OPC UA Browser** (tree view, node details, read/write)
- [ ] **Live Signals** (subscriptions with list/chart)
- [ ] **Run Wizard** (select TestPlan → execute → show progress)
- [ ] **Results Explorer** (filters, table, details, CSV/PDF export)
- [ ] Theme: basic CSS or MudBlazor (if allowed)

## 7) Security (Certificates)
- [ ] Client certificate store + trust list management UI
- [ ] Enforce `AutoAcceptUntrustedCertificates = false` (demo proper TLS)
- [ ] README section on certificate workflow (generate, trust, revoke)

## 8) In‑Process Mock OPC UA Server (optional)
- [ ] .NET worker hosting a minimal OPC UA server
- [ ] Define variables & two demo methods
- [ ] Deterministic value generator (for repeatable tests)

## 9) CI/CD
- [ ] GitHub Actions: restore/build/test + code coverage
- [ ] Spin up **Docker OPC UA reference server** for integration tests
- [ ] Publish `PlcLab.Web` as container image

## 10) Integration Tests
- [ ] Connect → browse → read/write → subscribe → method call
- [ ] Stable pass/fail scenarios for automated validation

## 11) Documentation
- [ ] README: overview, architecture diagram, screenshots/GIFs
- [ ] Quickstart for virtual PLC in Docker; endpoint configuration
- [ ] Security notes (certs, TLS, trust lists)
- [ ] Roadmap & known limitations

---

## Milestones
- [ ] M1: Repo scaffold + Docker ref server + basic connect/browse
- [ ] M2: Subscriptions + Run Wizard + minimal persistence
- [ ] M3: Method calls + Results Explorer + export
- [ ] M4: CI pipeline + integration tests (virtual PLC)
- [ ] M5 (optional): In‑process mock server + extra UI polish

## Nice-to-haves
- [ ] Health check and readiness probe (if using k8s later)
- [ ] Audit fields: certificate thumbprint, endpoint URL, user identity
- [ ] Global Discovery Server integration
- [ ] Feature flags for experimental UI
- [ ] Localization (CS/EN) and time zone handling
- [ ] Blazor UI - dark mode + responsive layout (MudBlazor or Bootstrap)

---

# ops/ — Operational Guides

This folder contains operational docs and snippets to manage the PlcLab project lifecycle: versioning, CI/CD, Docker, integration testing, certificates, and deployments.

Quick links:
- [ReleaseChecklist.md](ops/ReleaseChecklist.md)
- [CHANGELOG.md](ops/CHANGELOG.md)
- [CreateTagAndPush.md](ops/CreateTagAndPush.md)
- [DockerBuildAndPush.md](ops/DockerBuildAndPush.md)
- [LocalTestWithDocker.md](ops/LocalTestWithDocker.md)
- [DevSetup.md](ops/DevSetup.md)
- [CertificatesGuide.md](ops/CertificatesGuide.md)
- [RunMigrations.md](ops/RunMigrations.md)
- [StartReferenceServer.md](ops/StartReferenceServer.md)
- [RunIntegrationTests.md](ops/RunIntegrationTests.md)
- [DeployToAzure.md](ops/DeployToAzure.md)
- [DeployToK8s.md](ops/DeployToK8s.md)
- [Rollback.md](ops/Rollback.md)
