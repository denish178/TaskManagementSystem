import api from "./api";

const dashboardService = {
  async getProjectDashboard(projectId, signal) {
    const response = await api.get(`/api/dashboard/project/${projectId}`, {
      signal,
    });
    return response.data;
  },

  async getProjectStatusCounts(projectId, signal) {
    const response = await api.get(
      `/api/dashboard/project/${projectId}/status-counts`,
      { signal },
    );
    return response.data;
  },

  async getOverdueCount(projectId, signal) {
    const response = await api.get(
      `/api/dashboard/project/${projectId}/overdue`,
      { signal },
    );
    return response.data;
  },

  async getSprintProgress(sprintId, signal) {
    const response = await api.get(
      `/api/dashboard/sprint/${sprintId}/progress`,
      { signal },
    );
    return response.data;
  },
};

export default dashboardService;
