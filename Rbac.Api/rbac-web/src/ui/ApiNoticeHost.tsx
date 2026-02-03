import { useEffect, useState } from "react";
import { API_NOTICE_EVENT, type ApiNoticePayload } from "../api-notice";
import Toast from "./Toast";

export type ApiNotice = {
  id: string;
  variant: "default" | "success" | "warning" | "error";
  title?: string;
  message: string;
};

export default function ApiNoticeHost() {
  const [notices, setNotices] = useState<ApiNotice[]>([]);

  useEffect(() => {
    function handleNotice(event: Event) {
      const customEvent = event as CustomEvent<ApiNoticePayload>;
      if (!customEvent.detail?.message) return;

      const id =
        typeof crypto !== "undefined" && "randomUUID" in crypto
          ? crypto.randomUUID()
          : `${Date.now()}-${Math.random().toString(16).slice(2)}`;

      setNotices((previous) => [{ id, ...customEvent.detail }, ...previous].slice(0, 4));
    }

    window.addEventListener(API_NOTICE_EVENT, handleNotice);
    return () => window.removeEventListener(API_NOTICE_EVENT, handleNotice);
  }, []);

  useEffect(() => {
    if (notices.length === 0) return;

    const timer = window.setTimeout(() => {
      setNotices((current) => current.slice(0, -1));
    }, 5000);

    return () => window.clearTimeout(timer);
  }, [notices]);

  if (notices.length === 0) return null;

  return (
    <div className="toast-stack" aria-live="polite" aria-atomic="false">
      {notices.map((notice) => (
        <Toast
          key={notice.id}
          variant={notice.variant}
          title={notice.title}
          onClose={() => setNotices((current) => current.filter((item) => item.id !== notice.id))}
        >
          {notice.message}
        </Toast>
      ))}
    </div>
  );
}
