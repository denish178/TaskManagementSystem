import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Bell, LogOut, Menu, Moon, Sun, UserRound } from "lucide-react";
import { useAuth } from "../../context/AuthContext";
import { useTheme } from "../../context/ThemeContext";
import Avatar from "../common/Avatar";
import GlobalSearch from "./GlobalSearch";
import notificationService from "../../services/notificationService";
import { connectNotificationHub } from "../../services/realtimeService";
import {
  ensureNotificationPermission,
  showBrowserNotification,
} from "../../utils/browserNotifications";
import { formatDateTime } from "../../utils/format";
import { getField } from "../../utils/http";

export default function Navbar({ onMenu }) {
  const { user, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const [noticeOpen, setNoticeOpen] = useState(false);
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loadingNotices, setLoadingNotices] = useState(false);

  const loadNotifications = useCallback(async () => {
    setLoadingNotices(true);
    try {
      const [list, count] = await Promise.all([
        notificationService.list(),
        notificationService.unreadCount(),
      ]);
      setNotifications(list);
      setUnreadCount(count);
    } catch {
      setNotifications([]);
      setUnreadCount(0);
    } finally {
      setLoadingNotices(false);
    }
  }, []);

  useEffect(() => {
    loadNotifications();
    ensureNotificationPermission().catch(() => {});

    const hub = connectNotificationHub({
      onNotificationReceived: (payload) => {
        setNotifications((current) => {
          const id = payload.id || payload.Id;
          if (current.some((n) => (n.id || n.Id) === id)) return current;
          return [payload, ...current];
        });
        setUnreadCount((count) => count + 1);
        const title = payload.title || payload.Title || "TaskFlow";
        const message = payload.message || payload.Message || "";
        showBrowserNotification(title, {
          body: message,
          tag: String(payload.id || payload.Id),
        });
      },
    });

    hub.start().catch(() => {});

    const interval = setInterval(loadNotifications, 300_000);
    return () => {
      clearInterval(interval);
      hub.stop().catch(() => {});
    };
  }, [loadNotifications]);

  async function openNotifications() {
    setNoticeOpen((open) => !open);
    if (!noticeOpen) {
      await loadNotifications();
    }
  }

  async function handleMarkRead(notification) {
    const id = getField(notification, "id");
    if (!getField(notification, "isRead")) {
      await notificationService.markRead(id);
      await loadNotifications();
    }
    const link = getField(notification, "linkUrl");
    if (link) {
      navigate(link);
      setNoticeOpen(false);
    }
  }

  async function markAllRead() {
    await notificationService.markAllRead();
    await loadNotifications();
  }

  function handleLogout() {
    logout();
    navigate("/login", { replace: true });
  }

  return (
    <header className="topbar">
      <div className="flex items-center gap-3 px-4 py-3 lg:px-6">
        <button
          type="button"
          className="icon-btn lg:hidden"
          onClick={onMenu}
          aria-label="Open navigation"
        >
          <Menu size={18} />
        </button>
        <div className="hidden min-w-0 flex-1 sm:block">
          <GlobalSearch />
        </div>
        <div className="ml-auto flex items-center gap-2">
          <button
            type="button"
            className="icon-btn"
            onClick={toggleTheme}
            aria-label="Toggle theme"
          >
            {theme === "dark" ? <Sun size={18} /> : <Moon size={18} />}
          </button>
          <div className="relative">
            <button
              type="button"
              className="icon-btn relative"
              onClick={openNotifications}
              aria-label="Notifications"
            >
              <Bell size={18} />
              {unreadCount > 0 ? (
                <span
                  className="absolute right-1 top-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-rose-500 px-1 text-[10px] font-bold text-white"
                >
                  {unreadCount > 9 ? "9+" : unreadCount}
                </span>
              ) : null}
            </button>
            {noticeOpen ? (
              <div className="dropdown-panel absolute right-0 z-30 mt-2 w-80 max-h-96 overflow-y-auto p-3 text-sm">
                <div className="mb-2 flex items-center justify-between">
                  <p className="font-semibold">Notifications</p>
                  {unreadCount > 0 ? (
                    <button
                      type="button"
                      className="text-link text-xs"
                      onClick={markAllRead}
                    >
                      Mark all read
                    </button>
                  ) : null}
                </div>
                {loadingNotices ? (
                  <p className="text-slate-500">Loading…</p>
                ) : notifications.length === 0 ? (
                  <p className="text-slate-500">No notifications yet.</p>
                ) : (
                  <ul className="space-y-2">
                    {notifications.map((item) => {
                      const isRead = Boolean(getField(item, "isRead"));
                      return (
                        <li key={getField(item, "id")}>
                          <button
                            type="button"
                            className={`w-full rounded-md px-2 py-2 text-left hover:bg-[var(--app-bg)] ${
                              isRead ? "opacity-70" : "bg-[var(--app-accent-soft)]"
                            }`}
                            onClick={() => handleMarkRead(item)}
                          >
                            <p className="font-medium">{getField(item, "title")}</p>
                            <p className="text-xs text-slate-500">
                              {getField(item, "message")}
                            </p>
                            <p className="mt-1 text-[10px] text-slate-400">
                              {formatDateTime(getField(item, "createdAtUtc"))}
                            </p>
                          </button>
                        </li>
                      );
                    })}
                  </ul>
                )}
              </div>
            ) : null}
          </div>
          <div className="relative">
            <button
              type="button"
              className="flex items-center gap-2 rounded-md p-1.5 hover:bg-[var(--app-bg)]"
              onClick={() => setMenuOpen((open) => !open)}
              aria-label="Open profile menu"
            >
              <Avatar name={user?.name} size="sm" />
              <span className="hidden text-sm font-medium md:block">{user?.name}</span>
            </button>
            {menuOpen ? (
              <div className="dropdown-panel absolute right-0 mt-2 w-48 overflow-hidden py-1">
                <Link
                  to="/profile"
                  className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-[var(--app-bg)]"
                  onClick={() => setMenuOpen(false)}
                >
                  <UserRound size={16} />
                  Profile
                </Link>
                <button
                  type="button"
                  className="flex w-full items-center gap-2 px-3 py-2 text-sm text-[var(--app-danger)] hover:bg-[var(--app-bg)]"
                  onClick={handleLogout}
                >
                  <LogOut size={16} />
                  Logout
                </button>
              </div>
            ) : null}
          </div>
        </div>
      </div>
      <div className="px-4 pb-3 sm:hidden">
        <GlobalSearch />
      </div>
    </header>
  );
}
