import type { ReactNode } from "react";

type BadgeProps = {
  variant?: "default" | "success" | "warning" | "danger" | "info";
  children: ReactNode;
  className?: string;
};

export default function Badge({ variant = "default", children, className = "" }: BadgeProps) {
  return <span className={`badge badge--${variant} ${className}`.trim()}>{children}</span>;
}
