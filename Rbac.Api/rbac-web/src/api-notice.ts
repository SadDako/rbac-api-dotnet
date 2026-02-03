export type ApiNoticePayload = {
  variant: "default" | "success" | "warning" | "error";
  title?: string;
  message: string;
};

export const API_NOTICE_EVENT = "rbac:api-notice";

export function emitApiNotice(payload: ApiNoticePayload) {
  window.dispatchEvent(new CustomEvent<ApiNoticePayload>(API_NOTICE_EVENT, { detail: payload }));
}
