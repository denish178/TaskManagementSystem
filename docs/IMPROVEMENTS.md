# Improvement roadmap

## Phase 1–3

See git history / prior sections: access control, notifications, AI, SignalR Kanban/comments, audit log, Docker apps, Shared JWT.

## Phase 4 (implemented)

- **Redis SignalR backplane** — Optional `SignalR:Redis:Configuration` via `AddTaskManagementSignalR` in Shared; **Task**, **Comment**, **Notification**; `tms-redis` in `docker-compose.dev.yml`; `start-backend.ps1` sets `localhost:6379` when using script mode.
- **Real-time + browser push** — `NotificationHub` (`/hubs/notifications`); `NotificationReceived` event; navbar updates instantly; **desktop notifications** when tab is in background (permission prompt on login).
- **Docker hardening** — `Dockerfile.microservice` runs as `$APP_UID`, includes `curl` **HEALTHCHECK** on `/health`; compose apps wire Redis for SignalR scale-out.

## Phase 5 (next)

- Email notifications (SMTP / SendGrid)
- OpenTelemetry tracing across Gateway + services
- Kubernetes / Azure Container Apps deployment manifests
