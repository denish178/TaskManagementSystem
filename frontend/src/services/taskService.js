import api from "./api";
import { asArray } from "../utils/http";

const taskService = {
  async list(filters = {}, signal) {
    const response = await api.get("/api/tasks", {
      params: filters,
      signal,
    });
    return asArray(response.data);
  },

  async getById(taskId, signal) {
    const response = await api.get(`/api/tasks/${taskId}`, { signal });
    return response.data;
  },

  async create(payload) {
    const response = await api.post("/api/tasks", payload);
    return response.data;
  },

  async update(taskId, payload) {
    const response = await api.put(`/api/tasks/${taskId}`, payload);
    return response.data;
  },

  async remove(taskId) {
    await api.delete(`/api/tasks/${taskId}`);
  },

  async updateStatus(taskId, status) {
    const response = await api.patch(`/api/tasks/${taskId}/status`, { status });
    return response.data;
  },

  async assign(taskId, assigneeId) {
    const response = await api.patch(`/api/tasks/${taskId}/assign`, {
      assigneeId,
    });
    return response.data;
  },

  async getAudit(taskId, signal) {
    const response = await api.get(`/api/tasks/${taskId}/audit`, { signal });
    return asArray(response.data);
  },

  async listSubtasks(taskId, signal) {
    const response = await api.get(`/api/tasks/${taskId}/subtasks`, { signal });
    return asArray(response.data);
  },

  async createSubtask(taskId, payload) {
    const response = await api.post(`/api/tasks/${taskId}/subtasks`, payload);
    return response.data;
  },

  async updateSubtask(taskId, subtaskId, payload) {
    const response = await api.put(
      `/api/tasks/${taskId}/subtasks/${subtaskId}`,
      payload,
    );
    return response.data;
  },

  async removeSubtask(taskId, subtaskId) {
    await api.delete(`/api/tasks/${taskId}/subtasks/${subtaskId}`);
  },
};

export default taskService;
