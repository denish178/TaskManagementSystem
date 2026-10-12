# Task Management System

Microservices backend (API Gateway + 7 services), React frontend, and local-first development **without paid Azure resources**.

## Architecture

| Component | Technology | Local dev |
|-----------|------------|-----------|
| Gateway | Ocelot (`:5204`) | `backend/Gateway/TaskManagement.Gateway` |
| Auth | EF Core + SQL Server | `TMS_Auth` database |
| Project | EF Core + SQL Server | `TMS_Project` (teams, projects, sprints) |
| Task | EF Core + SQL Server | `TMS_Task` |
| Comment | MongoDB | `mongodb://localhost:27018` |
| File | EF Core + **local disk** storage | `TMS_File` + `App_Data/uploads` |
| Dashboard | HTTP → Task service | Aggregates task metrics |
| AI | Gemini + SQL context (`:5287`) | `POST /api/chat` via Gateway; scoped DB context; rate limited |
| Notification | EF Core + SQL Server (`:5290`) | `TMS_Notification`; in-app bell in UI |
| Shared | `TaskManagement.Shared` | JWT, health, rate-limit policies (Auth, AI, Notification) |
| Frontend | React + Vite (`:5173`) | `frontend` |

**Not required locally:** Azure SQL, Key Vault, Azure Blob (File service uses `LocalBlobStorageService` in Development).  
**Secrets:** copy `.env.example` to `.env` in the repo root (gitignored). Required for Docker SQL, JWT, dev seed users, and optional `GEMINI_API_KEY`.  
**AI chat:** set `GEMINI_API_KEY` in `.env` (or User Secrets `Gemini:ApiKey`) — see `frontend/README.md`.

Production continues to use `appsettings.json` + **User Secrets / environment variables** for Azure SQL, MongoDB Atlas, and Blob Storage.

**Note:** If you previously stored Azure connection strings in .NET User Secrets, they override `appsettings.Development.json`. Use `scripts/start-backend.ps1` (it sets local env vars per service) or clear secrets: `dotnet user-secrets clear --project <csproj>`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (SQL Server + MongoDB containers)

**Port conflicts**

- MongoDB: compose uses host port **27018** if **27017** is already in use.
- SQL Server: compose uses **1433**. If you already run SQL Server locally, stop it or change the compose port and update `appsettings.Development.json` connection strings.

## Quick start (recommended)

From the repository root:

```powershell
copy .env.example .env
# Edit .env — set MSSQL_SA_PASSWORD, JWT_SIGNING_KEY, TMS_DEV_* passwords
powershell -File scripts/start-local.ps1
```

In a **second terminal**:

```powershell
cd frontend
copy .env.example .env
npm install
npm run dev
```

| URL | Purpose |
|-----|---------|
| http://localhost:5173 | Frontend |
| http://localhost:5204 | API Gateway |
| http://localhost:5204/health | Gateway health |

### Dev login (seeded on first Auth startup)

Set `TMS_DEV_ADMIN_PASSWORD` and `TMS_DEV_USER_PASSWORD` in `.env`. Default emails:

| Email | Role |
|-------|------|
| admin@local.test | Admin |
| user@local.test | User |

Migrations run automatically in **Development** for Auth, Task, Project, File, and Notification.

## Scripts

| Script | Description |
|--------|-------------|
| `scripts/start-local.ps1` | Docker infra + all backend services (use `powershell -File`) |
| `scripts/start-backend.ps1` | Backend only (expects DBs running) |
| `scripts/stop-backend.ps1` | Stop processes started by `start-backend.ps1` |
| `scripts/wait-sql.ps1` | Wait until SQL port is ready |
| `scripts/health-check.ps1` | GET `/health` on each service |
| `scripts/e2e-gateway.ps1` | API smoke tests through the Gateway |
| `docker-compose.apps.yml` | Optional: run all .NET services in Docker (with `docker-compose.dev.yml`) |
| Redis (`tms-redis`, `:6379`) | Optional SignalR backplane (started with `docker-compose.dev.yml`) |

Logs: `logs/*.log`  
Process IDs: `.local/backend-pids.json`

## Manual startup

```powershell
docker compose -f docker-compose.dev.yml up -d
powershell -File scripts/wait-sql.ps1
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project backend/Services/Auth/TaskManagement.Auth
# ... repeat for Project, Task, Comment, File, Dashboard, AI, Notification, then Gateway
```

Ports match `backend/Gateway/TaskManagement.Gateway/ocelot.json`.

## Frontend environment

`frontend/.env.example`:

```env
VITE_API_BASE_URL=http://localhost:5204
```

## Business rules

- Kanban statuses: `Todo`, `InProgress`, `Testing`, `Done`
- **Only `Admin` may assign tasks** (`PATCH /api/tasks/{id}/assign`); UI hides assign controls for other roles
- Task updates do not change `AssigneeId` unless the assign endpoint is called

## Tests

```powershell
dotnet test backend/TaskManagement.slnx
cd frontend
npm run lint
npm run test
npm run build
npm run test:e2e   # Playwright; full login test needs backend + frontend running
```

## Improvements

See [docs/IMPROVEMENTS.md](docs/IMPROVEMENTS.md) for the security/DevOps roadmap (Phase 1 is in the codebase).

## More detail

See [docs/LOCAL_DEVELOPMENT.md](docs/LOCAL_DEVELOPMENT.md).
