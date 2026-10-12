import api from "./api";
import { asArray } from "../utils/http";

const projectService = {
  async list(signal) {
    const response = await api.get("/api/projects", { signal });
    return asArray(response.data);
  },

  async getById(projectId, signal) {
    const response = await api.get(`/api/projects/${projectId}`, { signal });
    return response.data;
  },

  async create(payload) {
    const response = await api.post("/api/projects", payload);
    return response.data;
  },

  async update(projectId, payload) {
    const response = await api.put(`/api/projects/${projectId}`, payload);
    return response.data;
  },

  async remove(projectId) {
    await api.delete(`/api/projects/${projectId}`);
  },
};

export default projectService;
