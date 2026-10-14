import { useEffect, useRef, useState } from "react";
import { MessageCircle, Send, X, Bot, UserRound } from "lucide-react";
import chatService from "../../services/chatService";
import Button from "../common/Button";

const STORAGE_KEY = "tms-chat-messages";

function loadStoredMessages() {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function formatAssistantText(text) {
  return text.split("\n").map((line, index) => (
    <span key={index}>
      {line}
      {index < text.split("\n").length - 1 ? <br /> : null}
    </span>
  ));
}

export default function FloatingChatWidget() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState(loadStoredMessages);
  const [draft, setDraft] = useState("");
  const [sending, setSending] = useState(false);
  const [error, setError] = useState("");
  const listRef = useRef(null);
  const inputRef = useRef(null);

  useEffect(() => {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(messages));
  }, [messages]);

  useEffect(() => {
    if (open && listRef.current) {
      listRef.current.scrollTop = listRef.current.scrollHeight;
    }
  }, [open, messages, sending]);

  useEffect(() => {
    if (open) {
      inputRef.current?.focus();
    }
  }, [open]);

  async function handleSend(event) {
    event.preventDefault();
    const text = draft.trim();
    if (!text || sending) return;

    setError("");
    setDraft("");
    const userMessage = { id: crypto.randomUUID(), role: "user", content: text };
    setMessages((prev) => [...prev, userMessage]);
    setSending(true);

    try {
      const answer = await chatService.sendMessage(text);
      setMessages((prev) => [
        ...prev,
        {
          id: crypto.randomUUID(),
          role: "assistant",
          content: answer || "No response from the assistant.",
        },
      ]);
    } catch (err) {
      const message =
        err.normalized?.message ||
        err.response?.data?.message ||
        "Unable to reach the assistant. Check that the AI service is running and Gemini is configured.";
      setError(message);
      setMessages((prev) => [
        ...prev,
        {
          id: crypto.randomUUID(),
          role: "assistant",
          content: message,
          isError: true,
        },
      ]);
    } finally {
      setSending(false);
    }
  }

  function clearChat() {
    setMessages([]);
    setError("");
    sessionStorage.removeItem(STORAGE_KEY);
  }

  return (
    <div className="pointer-events-none fixed bottom-4 right-4 z-50 flex flex-col items-end gap-3 sm:bottom-6 sm:right-6">
      {open ? (
        <section className="chat-panel" aria-label="Task assistant chat">
          <header className="flex items-center justify-between border-b border-[var(--app-border)] px-4 py-3">
            <div className="flex items-center gap-2">
              <span
                className="flex h-8 w-8 items-center justify-center rounded-md bg-[var(--app-accent)] text-white dark:text-stone-900"
                aria-hidden
              >
                <Bot size={16} />
              </span>
              <div>
                <p className="text-sm font-semibold">Task assistant</p>
                <p className="text-xs text-[var(--app-text-muted)]">
                  Projects, tasks, and sprints
                </p>
              </div>
            </div>
            <div className="flex items-center gap-1">
              <button type="button" className="icon-btn text-xs" onClick={clearChat}>
                Clear
              </button>
              <button
                type="button"
                className="icon-btn"
                onClick={() => setOpen(false)}
                aria-label="Close chat"
              >
                <X size={18} />
              </button>
            </div>
          </header>

          <div ref={listRef} className="flex-1 space-y-3 overflow-y-auto px-4 py-3">
            {messages.length === 0 ? (
              <p className="text-sm text-[var(--app-text-muted)]">
                Try: &quot;How many tasks are in Testing?&quot; or &quot;List my open
                projects.&quot;
              </p>
            ) : null}
            {messages.map((msg) => {
              const isUser = msg.role === "user";
              return (
                <div
                  key={msg.id}
                  className={`flex gap-2 ${isUser ? "justify-end" : "justify-start"}`}
                >
                  {!isUser ? (
                    <span
                      className="mt-1 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-[var(--app-accent-soft)] text-[var(--app-accent-text)]"
                      aria-hidden
                    >
                      <Bot size={14} />
                    </span>
                  ) : null}
                  <div
                    className={`max-w-[85%] rounded-lg px-3 py-2 text-sm ${
                      isUser
                        ? "bg-[var(--app-accent)] text-white dark:text-stone-900"
                        : msg.isError
                          ? "alert-error"
                          : "border border-[var(--app-border)] bg-[var(--app-bg)]"
                    }`}
                  >
                    {isUser ? msg.content : formatAssistantText(msg.content)}
                  </div>
                  {isUser ? (
                    <span
                      className="mt-1 flex h-7 w-7 shrink-0 items-center justify-center rounded-full border border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-text-muted)]"
                      aria-hidden
                    >
                      <UserRound size={14} />
                    </span>
                  ) : null}
                </div>
              );
            })}
            {sending ? (
              <p className="text-xs text-[var(--app-text-muted)] animate-pulse">Thinking…</p>
            ) : null}
          </div>

          <form
            onSubmit={handleSend}
            className="border-t border-[var(--app-border)] p-3"
          >
            {error ? (
              <p className="mb-2 text-xs text-[var(--app-danger)]">{error}</p>
            ) : null}
            <div className="flex gap-2">
              <input
                ref={inputRef}
                type="text"
                value={draft}
                onChange={(event) => setDraft(event.target.value)}
                placeholder="Ask about tasks, sprints, projects…"
                className="input min-w-0 flex-1"
                disabled={sending}
                aria-label="Chat message"
              />
              <Button type="submit" disabled={sending || !draft.trim()} aria-label="Send">
                <Send size={16} />
              </Button>
            </div>
          </form>
        </section>
      ) : null}

      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        className="chat-fab"
        aria-label={open ? "Close task assistant" : "Open task assistant"}
        aria-expanded={open}
      >
        {open ? <X size={22} /> : <MessageCircle size={22} />}
      </button>
    </div>
  );
}
