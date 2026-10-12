import { useState } from "react";
import { toast } from "react-toastify";
import { Paperclip } from "lucide-react";
import Button from "../common/Button";
import EmptyState from "../common/EmptyState";
import fileService from "../../services/fileService";
import { FILE_ALLOWED_EXTENSIONS, FILE_MAX_BYTES } from "../../utils/constants";
import { formatBytes, formatDateTime } from "../../utils/format";
import { getField } from "../../utils/http";

export default function FilePanel({
  taskId,
  files,
  currentUser,
  usersById,
  onChange,
}) {
  const [progress, setProgress] = useState(null);
  const [uploading, setUploading] = useState(false);

  async function onFileChange(event) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;

    const extension = `.${file.name.split(".").pop()?.toLowerCase()}`;
    if (!FILE_ALLOWED_EXTENSIONS.includes(extension)) {
      toast.error(`File type ${extension} is not allowed.`);
      return;
    }
    if (file.size > FILE_MAX_BYTES) {
      toast.error("File size cannot exceed 10 MB.");
      return;
    }

    setUploading(true);
    try {
      await fileService.upload({
        file,
        taskId,
        uploadedBy: currentUser.id,
        onUploadProgress: (event) => {
          if (event.total) {
            setProgress(Math.round((event.loaded / event.total) * 100));
          }
        },
      });
      toast.success("File uploaded");
      onChange();
    } catch (error) {
      toast.error(error.normalized?.message || "Upload failed.");
    } finally {
      setUploading(false);
      setProgress(null);
    }
  }

  async function download(file) {
    try {
      const response = await fileService.download(getField(file, "id"));
      const blobUrl = URL.createObjectURL(response.data);
      const link = document.createElement("a");
      link.href = blobUrl;
      link.download = getField(file, "fileName") || "download";
      link.click();
      URL.revokeObjectURL(blobUrl);
    } catch (error) {
      toast.error(error.normalized?.message || "Download failed.");
    }
  }

  async function remove(file) {
    try {
      await fileService.remove(getField(file, "id"));
      toast.success("Attachment deleted");
      onChange();
    } catch (error) {
      toast.error(error.normalized?.message || "Unable to delete file.");
    }
  }

  return (
    <section>
      <div className="mb-3 flex items-center justify-between gap-3">
        <h2 className="text-base font-semibold">Attachments</h2>
        <label className="inline-flex cursor-pointer items-center rounded-lg bg-indigo-600 px-3 py-2 text-sm font-semibold text-white">
          Upload
          <input
            type="file"
            className="sr-only"
            onChange={onFileChange}
            disabled={uploading}
          />
        </label>
      </div>
      {progress != null ? (
        <p className="mb-3 text-xs text-slate-500">Uploading {progress}%</p>
      ) : null}
      {files.length === 0 ? (
        <EmptyState
          icon={Paperclip}
          title="No attachments"
          description="Upload a file up to 10 MB. Allowed types include images, PDF, Office documents, and text."
        />
      ) : (
        <ul className="divide-y divide-slate-200 rounded-xl border border-slate-200 dark:divide-slate-800 dark:border-slate-800">
          {files.map((file) => {
            const uploader = usersById[getField(file, "uploadedBy")];
            return (
              <li
                key={getField(file, "id")}
                className="flex flex-wrap items-center justify-between gap-3 p-3"
              >
                <div>
                  <p className="text-sm font-medium">{getField(file, "fileName")}</p>
                  <p className="text-xs text-slate-500">
                    {formatBytes(getField(file, "fileSize"))} ·{" "}
                    {formatDateTime(getField(file, "uploadedAt"))} ·{" "}
                    {uploader?.name || "Unknown"}
                  </p>
                </div>
                <div className="flex gap-2">
                  <Button variant="secondary" onClick={() => download(file)}>
                    Download
                  </Button>
                  <Button variant="ghost" onClick={() => remove(file)}>
                    Delete
                  </Button>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
