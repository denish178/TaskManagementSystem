import * as signalR from "@microsoft/signalr";
import { authStorage } from "../utils/authStorage";

const apiBase = import.meta.env.VITE_API_BASE_URL || "http://localhost:5204";

function buildConnection(hubPath) {
  const token = authStorage.getToken();
  return new signalR.HubConnectionBuilder()
    .withUrl(`${apiBase}${hubPath}`, {
      accessTokenFactory: () => token || "",
    })
    .withAutomaticReconnect()
    .build();
}

export function connectTaskHub(projectId, handlers) {
  const connection = buildConnection("/hubs/tasks");
  connection.on("TaskStatusChanged", handlers.onStatusChanged);
  connection.on("TaskAssigned", handlers.onAssigned);

  let started = false;

  async function start() {
    if (started) return;
    await connection.start();
    await connection.invoke("JoinProject", projectId);
    started = true;
  }

  async function stop() {
    if (!started) return;
    try {
      await connection.invoke("LeaveProject", projectId);
    } catch {
      /* connection may already be down */
    }
    await connection.stop();
    started = false;
  }

  return { start, stop, connection };
}

export function connectCommentHub(taskId, handlers) {
  const connection = buildConnection("/hubs/comments");
  connection.on("CommentAdded", handlers.onCommentAdded);

  let started = false;

  async function start() {
    if (started) return;
    await connection.start();
    await connection.invoke("JoinTask", taskId);
    started = true;
  }

  async function stop() {
    if (!started) return;
    try {
      await connection.invoke("LeaveTask", taskId);
    } catch {
      /* ignore */
    }
    await connection.stop();
    started = false;
  }

  return { start, stop, connection };
}

export function connectNotificationHub(handlers) {
  const connection = buildConnection("/hubs/notifications");
  connection.on("NotificationReceived", handlers.onNotificationReceived);

  let started = false;

  async function start() {
    if (started) return;
    await connection.start();
    started = true;
  }

  async function stop() {
    if (!started) return;
    await connection.stop();
    started = false;
  }

  return { start, stop, connection };
}
