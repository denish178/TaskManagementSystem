import api from "./api";
import { asArray } from "../utils/http";

const notificationService = {
  async list(signal) {
    const response = await api.get("/api/notifications", { signal });
    return asArray(response.data);
  },

  async unreadCount(signal) {
    const response = await api.get("/api/notifications/unread-count", { signal });
    return response.data?.count ?? 0;
  },

  async markRead(id) {
    await api.patch(`/api/notifications/${id}/read`);
  },

  async markAllRead() {
    await api.patch("/api/notifications/read-all");
  },
};

export default notificationService;
