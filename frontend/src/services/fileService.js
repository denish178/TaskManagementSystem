import api from "./api";
import { asArray } from "../utils/http";

const fileService = {
  async listByTask(taskId, signal) {
    const response = await api.get(`/api/files/task/${taskId}`, { signal });
    return asArray(response.data);
  },

  async upload({ file, taskId, uploadedBy, onUploadProgress }) {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("taskId", taskId);
    formData.append("uploadedBy", uploadedBy);

    const response = await api.post("/api/files/upload", formData, {
      onUploadProgress,
    });
    return response.data;
  },

  async download(fileId) {
    const response = await api.get(`/api/files/${fileId}/download`, {
      responseType: "blob",
    });
    return response;
  },

  async remove(fileId) {
    await api.delete(`/api/files/${fileId}`);
  },
};

export default fileService;
