export function asArray(data) {
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.$values)) return data.$values;
  if (Array.isArray(data?.items)) return data.items;
  return [];
}

export function getField(source, ...keys) {
  if (!source || typeof source !== "object") return undefined;

  for (const key of keys) {
    const variants = [
      key,
      key.charAt(0).toLowerCase() + key.slice(1),
      key.charAt(0).toUpperCase() + key.slice(1),
    ];

    for (const variant of variants) {
      if (source[variant] !== undefined && source[variant] !== null) {
        return source[variant];
      }
    }
  }

  return undefined;
}

export function normalizeError(error) {
  const status = error?.response?.status;
  const data = error?.response?.data;

  if (typeof data === "string" && data.trim()) {
    return { status, message: data };
  }

  const message =
    data?.error ||
    data?.Error ||
    data?.message ||
    data?.Message ||
    data?.title ||
    data?.Title ||
    (Array.isArray(data?.errors)
      ? data.errors.join(" ")
      : null) ||
    (data?.errors && typeof data.errors === "object"
      ? Object.values(data.errors).flat().join(" ")
      : null) ||
    error?.message ||
    "Something went wrong. Please try again.";

  return { status, message };
}

export function isCancelError(error) {
  return error?.code === "ERR_CANCELED" || error?.name === "CanceledError";
}
