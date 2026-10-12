# Local development (no paid Azure)

## Dependencies

| Service | Local replacement |
|---------|-------------------|
| Azure SQL | SQL Server in Docker (`docker-compose.dev.yml`) |
| MongoDB (comments) | MongoDB in Docker |
| Azure Blob | **Local disk** (`FileStorage:Mode=Local` in File `appsettings.Development.json`) |
| Key Vault | Not used locally; copy `.env.example` → `.env` (repo root, gitignored) |
| AI (Gemini) | `TaskManagement.AI` on `:5287`, proxied at `/api/chat`; requires `GEMINI_API_KEY` or User Secrets |

## Start everything

```powershell
copy .env.example .env
powershell -File scripts/start-local.ps1
```

Or infrastructure only:

```powershell
docker compose -f docker-compose.dev.yml up -d
powershell -File scripts/wait-sql.ps1
```

## Run microservices manually (Development)

Set `ASPNETCORE_ENVIRONMENT=Development` and run each project (ports match `ocelot.json`):

| Service | Project | URL |
|---------|---------|-----|
| Gateway | `backend/Gateway/TaskManagement.Gateway` | http://localhost:5204 |
| Auth | `backend/Services/Auth/TaskManagement.Auth` | http://localhost:5007 |
| Project | `backend/Services/Project/TaskManagement.Project` | http://localhost:5012 |
| Task | `backend/Services/Task/TaskManagement.Task` | http://localhost:5266 |
| Comment | `backend/Services/Comment/TaskManagement.Comment` | http://localhost:5260 |
| File | `backend/Services/File/TaskManagement.File` | http://localhost:5144 |
| Dashboard | `backend/Services/Dashboard/TaskManagement.Dashboard` | http://localhost:5209 |
| AI | `backend/Services/AI/TaskManagement.AI` | http://localhost:5287 |

EF migrations apply automatically on startup in Development for Auth, Task, Project, and File.

## Seeded accounts (Development only)

| Email | Role |
|-------|------|
| admin@local.test | Admin |
| user@local.test | User |

Passwords: `TMS_DEV_ADMIN_PASSWORD` and `TMS_DEV_USER_PASSWORD` in `.env`.

## Frontend

```bash
cd frontend
cp .env.example .env
npm run dev
```

`VITE_API_BASE_URL=http://localhost:5204`

## Production

Use Azure SQL, MongoDB Atlas, Blob Storage, and **user secrets / App Configuration** — do not commit production connection strings. Development settings are isolated in `appsettings.Development.json`.
