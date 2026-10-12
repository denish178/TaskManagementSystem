import { useState } from "react";
import { Outlet } from "react-router-dom";
import FloatingChatWidget from "../chat/FloatingChatWidget";
import Navbar from "./Navbar";
import Sidebar from "./Sidebar";

export default function Layout() {
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
      <Sidebar
        collapsed={collapsed}
        onToggle={() => setCollapsed((value) => !value)}
        mobileOpen={mobileOpen}
        onClose={() => setMobileOpen(false)}
      />
      <div className={`min-h-screen ${collapsed ? "lg:pl-[76px]" : "lg:pl-64"}`}>
        <Navbar onMenu={() => setMobileOpen(true)} />
        <main className="px-4 py-6 lg:px-8">
          <Outlet />
        </main>
      </div>
      <FloatingChatWidget />
    </div>
  );
}
