import api from "./api";
import { asArray } from "../utils/http";

const sprintService = {
  async listByProject(projectId, signal) {
    const response = await api.get(`/api/sprints/project/${projectId}`, {
      signal,
    });
    return asArray(response.data);
  },

  async getById(sprintId, signal) {
    const response = await api.get(`/api/sprints/${sprintId}`, { signal });
    return response.data;
  },

  async create(payload) {
    const response = await api.post("/api/sprints", payload);
    return response.data;
  },

  async update(sprintId, payload) {
    const response = await api.put(`/api/sprints/${sprintId}`, payload);
    return response.data;
  },

  async remove(sprintId) {
    await api.delete(`/api/sprints/${sprintId}`);
  },
};

export default sprintService;
