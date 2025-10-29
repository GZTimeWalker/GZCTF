# GZ::CTF — AI Coding Agent Guide

GZ::CTF is a full‑stack, production‑ready CTF platform for competitions and practice, designed for extensibility (dynamic/static challenges, containers, dynamic flags), observability (health/metrics/traces), and operability (rate limiting, RBAC, pluggable captcha/storage/cache). Backend is ASP.NET Core 9 + EF Core (PostgreSQL), frontend is React 19 + Vite with real‑time SignalR.

## Development principles

- Exercise-related models/features are out of scope for now. Ignore those files when implementing features.
- Be conservative with database schema changes. If logic can be done in the frontend (calculation/UI), avoid adding backend fields or endpoints.
- Prefer existing extension points (RateLimiter, Captcha, Storage, Telemetry, SignalR) and use inline JSON fields (`Tags`/`Hints`/`Divisions`) to keep schema flexible and avoid premature hardening.
- Follow role-based access patterns: use `[RequireUser]`, `[RequireMonitor]`, `[RequireAdmin]`, `[RequireAdminOrToken]` attributes consistently.
- TCP-over-WebSocket proxy (`ProxyController`) enables browser access to challenge containers when `ContainerPortMappingType.PlatformProxy` is configured.

## Architecture map

- Backend `src/GZCTF` (ASP.NET Core):
  - Entry: `Program.cs` wires startup via `Extensions/Startup/*` (web host, DB, storage, cache/SignalR, identity, telemetry, services, web services).
  - Middlewares & maps: `Extensions/AppExtensions.cs` sets routing, rate limit, auth, request logging, health/metrics, SignalR hubs at `/hub/{user|monitor|admin}`, static/cached `index.html` via `UseIndexAsync()` and custom favicon via `UseCustomFavicon()`.
  - Data: EF Core PostgreSQL is mandatory. App exits if `ConnectionStrings:Database` missing (`DatabaseExtension.ConfigureDatabase`).
  - Caching: Redis optional; when absent falls back to in-memory (`ConfigureCacheAndSignalR`).
  - Storage: custom storage providers (local disk and S3) selected via connection string; default forced to `disk://path=./files` (`StorageExtension`).
  - Telemetry: Health on `/healthz` and Prometheus `/metrics` are served only on metrics port `3000` (`Server.MetricPort` and `TelemetryExtension`). App serves HTTP on `8080`.
  - Auth & roles: IdentityCore with cookies; use attributes `RequireUser`, `RequireMonitor`, `RequireAdmin`, `RequireAdminOrToken` (`Middlewares/PrivilegeAuthentication.cs`).
  - Rate limiting: Global sliding window + named policies (`Middlewares/RateLimiter.cs`), disabled by `DisableRateLimit=true`.
  - i18n: `Resources/` with `IStringLocalizer<Program>`; invalid model state returns JSON via `InvalidModelStateHandler`.
  - SignalR patterns: Strongly-typed hubs (`AdminHub`, `MonitorHub`, `UserHub`) with client interfaces (`IAdminClient`, `IMonitorClient`, `IUserClient`) for real-time notifications. Each hub validates permissions and groups clients by game ID.
  - Container proxy: `ProxyController` handles TCP-over-WebSocket for challenge access when `ContainerPortMappingType.PlatformProxy` is set. Supports Docker Swarm and Kubernetes with traffic capture capabilities.
- Frontend `src/GZCTF/ClientApp` (React + Mantine + Vite):
  - Dev server on `63000` with proxy to backend; configure backend URL via `VITE_BACKEND_URL` (defaults to `http://localhost:8080`) in `vite.config.mts`.
  - Build outputs to `ClientApp/build` and is copied to backend `wwwroot` during `dotnet publish`.
  - SpaProxy: Enabled via `Microsoft.AspNetCore.SpaProxy` in development; auto-starts Vite dev server when running `dotnet run`.

## Database model overview (core relations)

- Identity & users
  - `UserInfo` extends `IdentityUser<Guid>`; `Role` stored as int, requires confirmed email. One‑to‑many `UserInfo -> Submission` (FK set null on delete). Many‑to‑many with `Team` for membership.
- Teams and games
  - `Team` has many `Members` (users) and an owner `Captain`. Many‑to‑many `Team <-> Game` via `Participation` (join entity) to carry team‑in‑game state.
  - `Game` aggregates `GameEvents`, `GameNotices`, `GameChallenges`, `Submissions`, `Participations`, and `Divisions` (new dedicated table). `Divisions` entity manages division-specific configs including challenge visibility and scoring rules.
- Divisions & challenge configs
  - `Divisions`: Dedicated table for multi-division support with `DivisionChallengeConfig` join entity for per-division challenge configuration.
  - `DivisionChallengeConfig`: Manages per-division challenge settings (visibility, deadline, scoring).
  - `Participation` references a `Division`; supports division-scoped team participation and scoring.
- Participation and instances
  - `Participation` (PK: generated) joins `Team` and `Game`; holds `Instances` (challenge instances for the team), `Submissions`, `Members` (`UserParticipation`), optional `Writeup`, and optional `DivisionId`.
  - Many‑to‑many `Participation <-> GameChallenge` via `GameInstance` (composite key). `GameInstance` may reference a running `Container` (1‑1) and a `FlagContext` (dynamic flag) and is auto‑included with `Challenge`.
- Challenges & training
  - `GameChallenge` (derives `Challenge`) relates to `Game`, has `Flags`, `Submissions`, optional `Attachment` and `TestContainer`; `Hints` stored as JSON; indexed by `GameId`. Per-challenge `Deadline` field for submission cutoff.
  - `FirstSolves`: Tracks first blood for each challenge per-division or globally (team, user, submission time).
- Submissions & flags
  - `Submission.Status` persisted as string (compat/readability); navigations to `Team`, `User`, `GameChallenge` auto‑included for audit/monitor views.
  - `FlagContext` may reference an `Attachment` (SetNull on delete) to support static/dynamic flag sources.
- Storage and attachments
  - `Attachment` -> `LocalFile` (SetNull on delete) separates metadata from blob storage; integrates with pluggable storage via hash paths.
- Events, notices, content
  - `GameEvent` and `GameNotice` store structured `Values` as list JSON for schema‑light extensibility; `GameEvent` links optional `Team` and `User`.
  - `Post` (announcements) has optional `Author` (SetNull) and `Tags` as list JSON.
- Auxiliary tables
  - `UserParticipation` composite key `(GameId, TeamId, UserId)` accelerates membership queries.
  - `ApiToken` has `Creator` (Restrict on delete). `DataProtectionKeys` for ASP.NET DataProtection.

Design notes (flexibility vs performance)

- JSON converters for `List<string>`/`HashSet<string>` store tags/hints/divisions inline (avoid join tables; easy to evolve).
- Widespread `Navigation.AutoInclude()` on hot paths improves DX; `QuerySplittingBehavior.SplitQuery` is enabled to avoid cartesian explosion when including graphs.
- Delete behaviors prefer `SetNull`/`NoAction` to preserve audit trails and prevent cascade storms across instances/submissions.
- Many‑to‑many bridges (e.g., `Participation`, `GameInstance`) carry extra state and keep write paths efficient; Exercise-related bridges are currently ignored.

## Conventions & patterns

- Controllers return JSON `RequestResponse` on errors; model validation is centralized. Keep URLs lowercase (`AddRouting(LowercaseUrls=true)`).
- Use SignalR endpoints `/hub/user`, `/hub/monitor`, `/hub/admin`; Vite dev proxy forwards `/hub` as WebSocket.
- Permissions: Use `[RequireUser]`, `[RequireMonitor]`, `[RequireAdmin]`, `[RequireAdminOrToken]` attributes. New permissions: `RequireReview` (submission review) and `AffectDynamicScore` (dynamic scoring).
- Captcha pluggable via `CaptchaConfig.Provider`: `HashPow` (default) or `CloudflareTurnstile` (`Extensions/CaptchaExtension.cs`).
- Storage connection string prefixes: `disk://` (forced default), `aws.s3://`, `minio.s3://`, `azure.blobs://`. Self-maintenance storage ensures blob cleanup and consistency.
- Environment configuration prefix: `GZCTF_` (env vars override config).
- Role hierarchy: `Admin` (3) > `Monitor` (1) > `User` (0) > `Banned` (-1). Frontend `WithRole` component uses `RoleMap` for access control.
- Container management: Docker Swarm (`SwarmManager`) and Kubernetes (`KubernetesManager`) support with K3s/MinIO integration. Use `IContainerManager` interface for consistency.
- Task Status: Enum includes `Success`, `Failed`, `Pending`, `Running`, `Unhealthy`, `Degraded` statuses for container/service health.
- Divisions API: Endpoints for game-scoped division management with challenge configs. Division affects challenge visibility, scoring, and deadline enforcement.
- FirstSolves tracking: Separate table for first-blood metadata (team, user, timestamp); used for bonus scoring and statistics.
- TypeScript API generation: Run `pnpm genapi` in ClientApp to regenerate types from OpenAPI spec at `/openapi/v1.json`.
- Repository pattern: All data access through `IRepository<T>` interfaces with `RepositoryBase` implementation.
- Logging: Use `logger.SystemLog(message, status, level)` for structured logging with `TaskStatus` enum.
- Error handling: Return `RequestResponse` objects with localized messages; use `IStringLocalizer<Program>` for i18n.

## Dev workflows

- Prereqs: .NET 9 SDK, Node 24+, `pnpm`.
- Single-command dev (SpaProxy auto-starts Vite):
  - `dotnet run --project src/GZCTF/GZCTF.csproj`
  - Launch profile sets `ASPNETCORE_ENVIRONMENT=Development` and enables `Microsoft.AspNetCore.SpaProxy` (see `Properties/launchSettings.json`); Vite dev runs on `63000` and proxies to backend.
- Manual dual-terminal (fallback if SpaProxy is disabled):
  1. Backend API: `dotnet run --project src/GZCTF/GZCTF.csproj`
  2. Frontend dev: `cd src/GZCTF/ClientApp && pnpm install && pnpm dev`
- Build & publish (includes SPA build):
  - `dotnet publish src/GZCTF/GZCTF.csproj -c Release -o publish`
  - Run: `dotnet ./publish/GZCTF.dll`
- Tests (xUnit + coverlet):
  - `dotnet test src/GZCTF.Test/GZCTF.Test.csproj -v minimal /p:CollectCoverage=true`
  - `dotnet test src/GZCTF.Integration.Test/GZCTF.Integration.Test.csproj -v minimal /p:CollectCoverage=true` (requires Docker; uses Testcontainers for K3s, MinIO, PostgreSQL)
- Testing framework:
  - Unit tests: xUnit with `IRepository<T>` and service mocking; examples in `GZCTF.Test/UnitTests/`.
  - Integration tests: Testcontainers for Docker Swarm/Kubernetes, MinIO S3, PostgreSQL, Redis; examples in `GZCTF.Integration.Test/Tests/`. Covers dynamic container challenges, flag retrieval, storage operations, and repository data validation.
- EF Core migrations (PostgreSQL):
  - `dotnet ef migrations add <Name> --project src/GZCTF/GZCTF.csproj --startup-project src/GZCTF/GZCTF.csproj`
  - `dotnet ef database update`
  - OpenAPI (development only): JSON at `/openapi/v1.json`, Scalar UI at `/scalar/v1`.
  - Generate TS API types for frontend (requires backend dev OpenAPI):
    - `cd src/GZCTF/ClientApp && pnpm genapi`
- Testing patterns: xUnit with `ConfigServiceTest.cs` and `SignatureTest.cs` examples; use `Microsoft.AspNetCore.Mvc.Testing` for integration tests.

## Configuration tips

- Database is required; set `ConnectionStrings:Database` (env: `GZCTF_ConnectionStrings__Database`). App exits if missing.
- Redis optional: `ConnectionStrings:RedisCache` enables distributed cache + SignalR backplane.
- Storage: `ConnectionStrings:Storage` (see prefixes above). Default is local disk under `./files`.
- Telemetry: configure `Telemetry` to enable Prometheus/OpenTelemetry/Azure Monitor; `/metrics` and `/healthz` are bound to port `3000`.
- Forwarded headers: use `ForwardedOptions` (proxies, networks) when behind reverse proxy.

## Container management

- Port mapping types: `Default` (random host ports) vs `PlatformProxy` (TCP-over-WebSocket).
- Container providers: Docker Swarm (`SwarmManager`) and Kubernetes (`KubernetesManager`) with `IContainerManager` abstraction.
- Traffic capture: Optional recording of container network traffic to storage when `EnableTrafficCapture=true`.
- Challenge types: Static/Dynamic containers with environment variable flag injection.
- Resource management: Automatic cleanup and scaling based on team participation.

## Security patterns

- CSP: Backend injects nonces at runtime for script/style integrity (`%nonce%` placeholders in `index.html`).
- Authentication: Cookie-based with IdentityCore; role-based authorization via attributes.
- Rate limiting: Sliding window policies with Redis-backed distributed state.
- Input validation: Centralized model validation with localized error messages.
- File uploads: Hash-based storage with configurable retention and access control.

## Frontend specifics

- Architecture & boundaries
  - Routing: file-based via `vite-plugin-pages` scanning `src/GZCTF/ClientApp/src/pages/**/*.tsx` (see `vite.config.mts`).
  - UI/Styles: Mantine (`@mantine/*`), CSS Modules (e.g., `src/.../styles/pages/About.module.css`), theme customizable.
  - Data fetching: `axios` + `swr`; typed API generated by `pnpm genapi` from `/openapi/v1.json`.
  - Real-time: SignalR client `@microsoft/signalr` talks to `/hub/{user|monitor|admin}`; WSRX (`@xdsec/wsrx`) for TCP-over-WebSocket challenge entry.
  - i18n: `i18next` + `react-i18next` with virtual manifest plugin (`plugins/vite-i18n-virtual-manifest`).
  - Enhancements: Shiki for code highlighting, ECharts for charts, `react-pdf` for PDFs.
  - Key dirs: `src/pages` (routes), `src/components` (shared UI like `WriteupSubmitModal.tsx`, `WsrxManager.tsx`), `src/styles`, `plugins/` (custom Vite plugins).
- Common patterns
  - Hooks: `useUser()`, `useUserRole()`, `useTeam()` for state management; `useTranslation()` for i18n.
  - Components: `WithRole` for access control, `ActionIconWithConfirm` for destructive actions, `WithGameMonitor` for game-specific pages.
  - Notifications: Use `showNotification()` from `@mantine/notifications` for user feedback.
  - Forms: Mantine forms with validation; use `useForm()` hook for complex forms.
- Dev server: Vite runs on `63000` with proxy to backend (`vite.config.mts`); preview `64000`.
- Proxy: forwards `/api`, `/swagger`, `/assets`, `/favicon.webp`, and `/hub` (ws).
- CSR security: Keep `%nonce%` in `index.html`; backend injects CSP nonce at runtime.
