import api from "./api";

const chatService = {
  async sendMessage(message, signal) {
    const response = await api.post(
      "/api/chat",
      { message },
      { signal, timeout: 120000 },
    );
    const data = response.data;
    return data.answer ?? data.Answer ?? "";
  },
};

export default chatService;
