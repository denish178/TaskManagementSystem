export async function ensureNotificationPermission() {
  if (!("Notification" in window)) {
    return "unsupported";
  }
  if (Notification.permission === "granted") {
    return "granted";
  }
  if (Notification.permission === "denied") {
    return "denied";
  }
  return Notification.requestPermission();
}

export function showBrowserNotification(title, options = {}) {
  if (!("Notification" in window) || Notification.permission !== "granted") {
    return;
  }
  if (!document.hidden) {
    return;
  }
  const notification = new Notification(title, {
    body: options.body,
    tag: options.tag || "taskflow",
  });
  if (options.onClick) {
    notification.onclick = options.onClick;
  }
}
