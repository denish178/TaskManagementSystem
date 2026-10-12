import api from "./api";
import { asArray } from "../utils/http";

const teamService = {
  async list(signal) {
    const response = await api.get("/api/teams", { signal });
    return asArray(response.data);
  },

  async getById(teamId, signal) {
    const response = await api.get(`/api/teams/${teamId}`, { signal });
    return response.data;
  },

  async create(payload) {
    const response = await api.post("/api/teams", payload);
    return response.data;
  },

  async update(teamId, payload) {
    const response = await api.put(`/api/teams/${teamId}`, payload);
    return response.data;
  },

  async remove(teamId) {
    await api.delete(`/api/teams/${teamId}`);
  },

  async getMembers(teamId, signal) {
    const response = await api.get(`/api/teams/${teamId}/members`, { signal });
    return asArray(response.data);
  },

  async addMember(teamId, payload) {
    const response = await api.post(`/api/teams/${teamId}/members`, payload);
    return response.data;
  },

  async removeMember(teamId, userId) {
    await api.delete(`/api/teams/${teamId}/members/${userId}`);
  },
};

export default teamService;
