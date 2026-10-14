import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Search } from "lucide-react";
import useDebounce from "../../hooks/useDebounce";
import projectService from "../../services/projectService";
import taskService from "../../services/taskService";
import authService from "../../services/authService";
import { getField } from "../../utils/http";

const EMPTY_RESULTS = {
  projects: [],
  tasks: [],
  users: [],
};

export default function GlobalSearch() {
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [results, setResults] = useState(EMPTY_RESULTS);

  const containerRef = useRef(null);
  const debounced = useDebounce(query, 300);
  const searchTerm = debounced.trim();
  const hasQuery = Boolean(searchTerm);

  useEffect(() => {
    function handleOutsideClick(event) {
      if (!containerRef.current?.contains(event.target)) {
        setOpen(false);
      }
    }

    window.addEventListener("mousedown", handleOutsideClick);

    return () => {
      window.removeEventListener("mousedown", handleOutsideClick);
    };
  }, []);

  useEffect(() => {
    if (!searchTerm) {
      return undefined;
    }

    const controller = new AbortController();
    const { signal } = controller;

    async function search() {
      // Defer state updates until the asynchronous task starts.
      await Promise.resolve();

      if (signal.aborted) return;

      setLoading(true);
      setError("");

      try {
        const [projects, tasks, users] = await Promise.all([
          projectService.list(signal),
          taskService.list({}, signal),
          authService.getUsers(signal),
        ]);

        if (signal.aborted) return;

        const needle = searchTerm.toLowerCase();

        setResults({
          projects: projects
            .filter((item) =>
              String(getField(item, "name") || "")
                .toLowerCase()
                .includes(needle),
            )
            .slice(0, 5),

          tasks: tasks
            .filter((item) =>
              String(getField(item, "title") || "")
                .toLowerCase()
                .includes(needle),
            )
            .slice(0, 5),

          users: users
            .filter((item) =>
              `${item.name || ""} ${item.email || ""}`
                .toLowerCase()
                .includes(needle),
            )
            .slice(0, 5),
        });
      } catch (err) {
        if (!signal.aborted && err.code !== "ERR_CANCELED") {
          setError(err.normalized?.message || "Search failed.");
        }
      } finally {
        if (!signal.aborted) {
          setLoading(false);
        }
      }
    }

    search();

    return () => controller.abort();
  }, [searchTerm]);

  const visibleResults = hasQuery ? results : EMPTY_RESULTS;
  const visibleError = hasQuery ? error : "";

  const empty =
    hasQuery &&
    !loading &&
    !visibleError &&
    visibleResults.projects.length +
      visibleResults.tasks.length +
      visibleResults.users.length ===
      0;

  function go(path) {
    setOpen(false);
    setQuery("");
    navigate(path);
  }

  return (
    <div ref={containerRef} className="relative w-full max-w-xl">
      <label className="sr-only" htmlFor="global-search">
        Search projects, tasks, and users
      </label>

      <Search
        className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[var(--app-text-muted)]"
        size={16}
      />

      <input
        id="global-search"
        value={query}
        onChange={(event) => {
          setQuery(event.target.value);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        placeholder="Search projects, tasks, users"
        autoComplete="off"
        className="search-input"
      />

      {open && hasQuery && (
        <div className="dropdown-panel absolute z-30 mt-2 w-full overflow-hidden">
          {loading && (
            <p className="px-4 py-3 text-sm text-slate-500">
              Searching...
            </p>
          )}

          {visibleError && (
            <p role="alert" className="px-4 py-3 text-sm text-rose-600">
              {visibleError}
            </p>
          )}

          {empty && (
            <p className="px-4 py-3 text-sm text-slate-500">
              No matching results.
            </p>
          )}

          {!loading && !visibleError && (
            <>
              <ResultGroup
                title="Projects"
                items={visibleResults.projects}
                getLabel={(item) => getField(item, "name")}
                onSelect={(item) => go(`/projects/${getField(item, "id")}`)}
              />

              <ResultGroup
                title="Tasks"
                items={visibleResults.tasks}
                getLabel={(item) => getField(item, "title")}
                onSelect={(item) => go(`/tasks/${getField(item, "id")}`)}
              />

              <ResultGroup
                title="Users"
                items={visibleResults.users}
                getLabel={(item) => item.name || item.email || "User"}
                onSelect={() => go("/profile")}
              />
            </>
          )}
        </div>
      )}
    </div>
  );
}

function ResultGroup({ title, items, getLabel, onSelect }) {
  if (!items.length) return null;

  return (
    <div className="border-t border-slate-100 p-2 dark:border-slate-800">
      <p className="px-2 py-1 text-[11px] font-semibold uppercase tracking-wide text-slate-400">
        {title}
      </p>

      {items.map((item) => (
        <button
          key={getField(item, "id") || getLabel(item)}
          type="button"
          className="block w-full rounded-lg px-2 py-2 text-left text-sm hover:bg-slate-50 dark:hover:bg-slate-800"
          onClick={() => onSelect(item)}
        >
          {getLabel(item)}
        </button>
      ))}
    </div>
  );
}