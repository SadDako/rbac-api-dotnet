import type { ReactNode } from "react";

type ToastProps = {
  variant?: "default" | "success" | "warning" | "error";
  title?: string;
  children?: ReactNode;
  onClose?: () => void;
};

export default function Toast({ variant = "default", title, children, onClose }: ToastProps) {
  return (
    <div className={`toast toast--${variant}`.trim()} role="status">
      <div className="toast__content">
        {title && <strong>{title}</strong>}
        {children && <p>{children}</p>}
      </div>
      {onClose && (
        <button className="toast__close" onClick={onClose} aria-label="Fechar aviso">
          ×
        </button>
      )}
    </div>
  );
}
