export const TASK_STATUSES = [
  { value: "Todo", label: "To Do" },
  { value: "InProgress", label: "In Progress" },
  { value: "Testing", label: "Testing" },
  { value: "Done", label: "Done" },
];

export const TASK_PRIORITIES = [
  { value: "Low", label: "Low" },
  { value: "Medium", label: "Medium" },
  { value: "High", label: "High" },
  { value: "Critical", label: "Critical" },
];

export const STATUS_COLORS = {
  Todo: "#78716c",
  InProgress: "#0f766e",
  Testing: "#b45309",
  Done: "#047857",
};

export const FILE_MAX_BYTES = 10 * 1024 * 1024;
export const FILE_ALLOWED_EXTENSIONS = [
  ".jpg",
  ".jpeg",
  ".png",
  ".pdf",
  ".doc",
  ".docx",
  ".xls",
  ".xlsx",
  ".txt",
];

export const ADMIN_ROLE = "Admin";
