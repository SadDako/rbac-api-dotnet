import type { ReactNode } from "react";

type AlertProps = {
  variant?: "default" | "error" | "warning" | "success" | "info";
  title?: string;
  children?: ReactNode;
  className?: string;
};

export default function Alert({
  variant = "default",
  title,
  children,
  className = "",
}: AlertProps) {
  return (
    <div className={`alert alert--${variant} ${className}`.trim()} role="status">
      {title && <strong>{title}</strong>}
      {children && <p>{children}</p>}
    </div>
  );
}
