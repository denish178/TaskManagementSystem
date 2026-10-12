import api from "./api";
import { authStorage } from "../utils/authStorage";
import { asArray } from "../utils/http";

const authService = {
  async login(email, password) {
    const response = await api.post("/api/auth/login", { email, password });
    const { token, user } = response.data;
    authStorage.setSession(token, user);
    return user;
  },

  async register(payload) {
    const response = await api.post("/api/auth/register", payload);
    return response.data;
  },

  async getProfile() {
    const response = await api.get("/api/auth/profile");
    authStorage.setSession(authStorage.getToken(), response.data);
    return response.data;
  },

  async updateProfile(payload) {
    const response = await api.put("/api/auth/profile", payload);
    authStorage.setSession(authStorage.getToken(), {
      ...authStorage.getUser(),
      ...response.data,
    });
    return response.data;
  },

  async changePassword(payload) {
    const response = await api.put("/api/auth/change-password", payload);
    return response.data;
  },

  async getUsers(signal) {
    const response = await api.get("/api/auth/users", { signal });
    return asArray(response.data);
  },

  async getUserById(userId, signal) {
    const response = await api.get(`/api/auth/users/${userId}`, { signal });
    return response.data;
  },

  logout() {
    authStorage.clear();
  },

  getStoredUser() {
    return authStorage.getUser();
  },

  isAuthenticated() {
    return Boolean(authStorage.getToken());
  },
};

export default authService;
