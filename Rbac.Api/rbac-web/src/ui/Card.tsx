import type { ReactNode } from "react";

type CardProps = {
  title?: string;
  description?: string;
  className?: string;
  children: ReactNode;
};

export default function Card({ title, description, className = "", children }: CardProps) {
  return (
    <section className={`card ${className}`.trim()}>
      {(title || description) && (
        <header className="card__header">
          {title && <h3>{title}</h3>}
          {description && <p>{description}</p>}
        </header>
      )}
      <div className="card__body">{children}</div>
    </section>
  );
}
