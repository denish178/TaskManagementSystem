# TaskFlow Frontend

Production-oriented React client for the Task Management System. All API calls go through the **API Gateway** (`VITE_API_BASE_URL`), not individual microservices.

## Prerequisites

- Node.js 20+
- API Gateway running (default `http://localhost:5204`)
- Backend microservices configured in `backend/Gateway/TaskManagement.Gateway/ocelot.json`

## Setup

```bash
cd frontend
cp .env.example .env
npm install
```

Set `VITE_API_BASE_URL` to your Gateway URL (no trailing slash).

## Scripts

| Command | Description |
|--------|-------------|
| `npm run dev` | Vite dev server (`http://localhost:5173`) |
| `npm run build` | Production build to `dist/` |
| `npm run preview` | Preview production build |
| `npm run lint` | ESLint |
| `npm run test` | Vitest unit tests |

## Authentication

- JWT login via `POST /api/auth/login`
- Token stored in **sessionStorage** (cleared when the tab closes)
- Protected routes redirect to `/login`
- Task assignment UI is shown only when `user.role === "Admin"` (backend enforces `PATCH /api/tasks/{id}/assign`)

## Deployment

- Build output: `dist/`
- `public/staticwebapp.config.json` provides SPA fallback for Azure Static Web Apps
- For App Service, configure the site to serve `index.html` for unknown routes

## Integrated services (via Gateway)

| Area | Routes |
|------|--------|
| Auth | `/api/auth/*` |
| Projects / Teams / Sprints | `/api/projects`, `/api/teams`, `/api/sprints` |
| Tasks / Subtasks | `/api/tasks` |
| Comments | `/api/comments` |
| Files | `/api/files` |
| Dashboard | `/api/dashboard` |
| AI assistant | `/api/chat` (floating widget on authenticated pages) |

**Not integrated:** Notification service; the bell shows an empty state until a notification API is routed in Ocelot.

### AI assistant (Gemini)

The floating chat widget calls `POST /api/chat` through the Gateway. Configure a [Google Gemini API key](https://aistudio.google.com/apikey) before chatting:

```powershell
$env:GEMINI_API_KEY = "your-key-here"
powershell -File scripts/start-local.ps1
```

Or: `dotnet user-secrets set "Gemini:ApiKey" "your-key" --project backend/Services/AI/TaskManagement.AI`

## Local development checklist

See [docs/LOCAL_DEVELOPMENT.md](../../docs/LOCAL_DEVELOPMENT.md) for Docker-based SQL/Mongo, seeded dev users, and service ports.

1. Start infrastructure (`docker compose -f docker-compose.dev.yml up -d`) and all Gateway-backed services in **Development**.
2. Run `npm run dev` and sign in with `admin@local.test` and the password from repo-root `.env` (`TMS_DEV_ADMIN_PASSWORD`).
3. Optional Gateway smoke test: `powershell -File scripts/e2e-gateway.ps1` (requires all services running).
