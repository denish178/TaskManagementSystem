import api from "./api";
import { asArray } from "../utils/http";

const commentService = {
  async listByTask(taskId, signal) {
    const response = await api.get(`/api/comments/task/${taskId}`, { signal });
    return asArray(response.data);
  },

  async create(payload) {
    const response = await api.post("/api/comments", payload);
    return response.data;
  },

  async update(commentId, payload) {
    const response = await api.put(`/api/comments/${commentId}`, payload);
    return response.data;
  },

  async remove(commentId) {
    await api.delete(`/api/comments/${commentId}`);
  },

  async getCompletionFeedback(taskId, signal) {
    const response = await api.get(
      `/api/comments/task/${taskId}/completion-feedback`,
      { signal },
    );
    return response.data;
  },

  async submitCompletionFeedback(taskId, content) {
    const response = await api.put(
      `/api/comments/task/${taskId}/completion-feedback`,
      { content },
    );
    return response.data;
  },
};

export default commentService;
