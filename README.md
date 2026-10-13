# Task Management System

A task board app: **React UI** talks to a **.NET API Gateway**, which routes to small backend services (auth, projects, tasks, comments, files, notifications, dashboard, AI chat).

You can run everything on your machine with **Docker + free local databases** — no Azure account required for development.

---

## What you need

1. **Docker Desktop** (SQL Server + MongoDB in containers)
2. **.NET 10 SDK**
3. **Node.js 20+**

---

## Run locally (3 steps)

### 1. Secrets

From the **repo root**:

```powershell
copy .env.example .env
```

Open `.env` and set at least:

- `MSSQL_SA_PASSWORD` — strong password for local SQL
- `JWT_SIGNING_KEY` — at least 32 characters
- `TMS_DEV_ADMIN_PASSWORD` / `TMS_DEV_USER_PASSWORD` — passwords for test logins

Optional: `GEMINI_API_KEY` for the AI chat widget.

### 2. Backend + databases

```powershell
powershell -File scripts/start-local.ps1
```

Wait until it finishes (Docker starts, then all services). Gateway health: http://localhost:5204/health

### 3. Frontend (new terminal)

```powershell
cd frontend
copy .env.example .env
npm install
npm run dev
```

Open **http://localhost:5173**

| What | URL |
|------|-----|
| App (UI) | http://localhost:5173 |
| API (Gateway) | http://localhost:5204 |

---

## Log in (dev users)

Passwords come from your `.env` (`TMS_DEV_ADMIN_PASSWORD` / `TMS_DEV_USER_PASSWORD`).

| Email | Role | Use for |
|-------|------|---------|
| `admin@local.test` | **Admin** | Full setup: teams, projects, sprints, members, assign tasks, delete tasks |
| `user@local.test` | **User** | Day-to-day work: view boards, move tasks, comments, files |

---

## Admin vs User (simple)

**Admin only**

- Create / edit / delete **teams** and **projects**
- Create / edit / delete **sprints**
- Add or remove **team members**
- **Assign** tasks to people
- Add or remove **subtasks**; **delete** tasks

**Both Admin and User** (when they have access to the project)

- View projects and tasks
- Create tasks on the board
- Change task status (Kanban), edit task details
- Mark subtasks complete (checkbox)
- Comments and file uploads
- Notifications bell, dashboard, AI chat (if Gemini key is set)

Task statuses: `Todo` → `InProgress` → `Testing` → `Done`

---

## Useful commands

| Goal | Command |
|------|---------|
| Stop backend services | `powershell -File scripts/stop-backend.ps1` |
| Check all service health | `powershell -File scripts/health-check.ps1` |
| API smoke test | `powershell -File scripts/e2e-gateway.ps1` |
| Backend tests | `dotnet test TaskManagement.slnx` |
| Frontend lint / test / build | `cd frontend` then `npm run lint`, `npm run test`, `npm run build` |

Frontend must point at the gateway: `VITE_API_BASE_URL=http://localhost:5204` in `frontend/.env`.

---

## Project layout (short)

```
TaskManagement.slnx     # Open in Visual Studio / Rider
backend/                # Microservices + Gateway
frontend/               # React app
deploy/docker/          # Docker Compose files
scripts/                # start-local.ps1, etc.
docs/                   # More detail
```

---

## More documentation

- [Local development](docs/LOCAL_DEVELOPMENT.md) — ports, manual service startup, frontend scripts
- [Repository layout](docs/REPOSITORY_LAYOUT.md) — folder conventions
- [Roadmap](docs/IMPROVEMENTS.md) — planned improvements (email, observability, deploy)

---

## Production note

Development uses `.env` and local Docker. Production should use environment variables / your host’s secret store for SQL, MongoDB, blob storage, and JWT — never commit real secrets to git.
