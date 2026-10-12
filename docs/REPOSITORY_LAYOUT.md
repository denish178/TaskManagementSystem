# Repository layout

Production-oriented monorepo: **backend** (.NET microservices), **frontend** (React), **deploy** (containers), **docs**, **scripts** (local dev only).

```
TaskManagementSystem/
├── TaskManagement.slnx          # .NET solution (repo root)
├── .env.example                 # Local secrets template (copy → .env, gitignored)
├── README.md
├── .github/workflows/           # CI
├── backend/
│   ├── Directory.Build.props    # Shared MSBuild settings
│   ├── Gateway/                 # Ocelot API gateway
│   ├── Services/                # One folder per bounded context
│   │   ├── Auth/TaskManagement.Auth/
│   │   ├── Project/…
│   │   ├── Task/…
│   │   └── …
│   ├── Shared/TaskManagement.Shared/
│   └── Tests/                   # xUnit per service (flat test projects)
├── frontend/                    # Vite + React SPA
├── deploy/
│   └── docker/                  # Dockerfile + compose (infra + optional full stack)
├── docs/                        # Architecture, local dev, roadmap
└── scripts/                     # PowerShell helpers (not used in production images)
```

## Conventions

| Area | Pattern |
|------|---------|
| Microservice | `backend/Services/{Name}/TaskManagement.{Name}/` — one deployable per folder |
| Gateway | `backend/Gateway/TaskManagement.Gateway/` |
| Tests | `backend/Tests/TaskManagement.{Name}.Tests/` — no nested duplicate folder names |
| Config | `appsettings.json` + env vars in prod; `appsettings.Development.json` for local only |
| Secrets | Never in git — repo-root `.env` or host/Key Vault in production |

## What is *not* wrong for production

- **Double segment** in service paths (`Services/Auth/TaskManagement.Auth`) matches .NET template naming; renaming every project is optional and high churn.
- **`scripts/`** and **`docs/`** stay in the repo for contributors; they are excluded from Docker images via build context (`COPY backend/` only).
- **Frontend tests** under `frontend/src/test/` and **e2e** under `frontend/e2e/` — common for Vite apps.

## Future (optional)

- `deploy/kubernetes/` or Azure Container Apps manifests (Phase 5)
- Email / OpenTelemetry in services (see `IMPROVEMENTS.md`)
