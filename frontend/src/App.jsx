import { lazy, Suspense } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import Layout from "./components/layout/Layout";
import ProtectedRoute from "./components/layout/ProtectedRoute";
import PublicOnlyRoute from "./components/layout/PublicOnlyRoute";
import Skeleton from "./components/common/Skeleton";
import Login from "./pages/Login";
import Register from "./pages/Register";
import NotFound from "./pages/NotFound";

const Dashboard = lazy(() => import("./pages/Dashboard"));
const Projects = lazy(() => import("./pages/Projects"));
const ProjectOverview = lazy(() => import("./pages/ProjectOverview"));
const ProjectTasks = lazy(() => import("./pages/ProjectTasks"));
const ProjectSprints = lazy(() => import("./pages/ProjectSprints"));
const ProjectMembers = lazy(() => import("./pages/ProjectMembers"));
const ProjectFiles = lazy(() => import("./pages/ProjectFiles"));
const TaskDetails = lazy(() => import("./pages/TaskDetails"));
const MyWorkspace = lazy(() => import("./pages/MyWorkspace"));
const MyTasks = lazy(() => import("./pages/MyTasks"));
const Profile = lazy(() => import("./pages/Profile"));

function PageFallback() {
  return <Skeleton className="h-64" />;
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/login"
          element={
            <PublicOnlyRoute>
              <Login />
            </PublicOnlyRoute>
          }
        />
        <Route
          path="/register"
          element={
            <PublicOnlyRoute>
              <Register />
            </PublicOnlyRoute>
          }
        />

        <Route
          element={
            <ProtectedRoute>
              <Layout />
            </ProtectedRoute>
          }
        >
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route
            path="/dashboard"
            element={
              <Suspense fallback={<PageFallback />}>
                <Dashboard />
              </Suspense>
            }
          />
          <Route
            path="/projects"
            element={
              <Suspense fallback={<PageFallback />}>
                <Projects />
              </Suspense>
            }
          />
          <Route
            path="/projects/:projectId"
            element={
              <Suspense fallback={<PageFallback />}>
                <ProjectOverview />
              </Suspense>
            }
          />
          <Route
            path="/projects/:projectId/tasks"
            element={
              <Suspense fallback={<PageFallback />}>
                <ProjectTasks />
              </Suspense>
            }
          />
          <Route
            path="/projects/:projectId/sprints"
            element={
              <Suspense fallback={<PageFallback />}>
                <ProjectSprints />
              </Suspense>
            }
          />
          <Route
            path="/projects/:projectId/members"
            element={
              <Suspense fallback={<PageFallback />}>
                <ProjectMembers />
              </Suspense>
            }
          />
          <Route
            path="/projects/:projectId/files"
            element={
              <Suspense fallback={<PageFallback />}>
                <ProjectFiles />
              </Suspense>
            }
          />
          <Route
            path="/tasks/:taskId"
            element={
              <Suspense fallback={<PageFallback />}>
                <TaskDetails />
              </Suspense>
            }
          />
          <Route
            path="/my-workspace"
            element={
              <Suspense fallback={<PageFallback />}>
                <MyWorkspace />
              </Suspense>
            }
          />
          <Route path="/workspace" element={<Navigate to="/my-workspace" replace />} />
          <Route
            path="/my-tasks"
            element={
              <Suspense fallback={<PageFallback />}>
                <MyTasks />
              </Suspense>
            }
          />
          <Route
            path="/profile"
            element={
              <Suspense fallback={<PageFallback />}>
                <Profile />
              </Suspense>
            }
          />
        </Route>

        <Route path="*" element={<NotFound />} />
      </Routes>
    </BrowserRouter>
  );
}
