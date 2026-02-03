import type { InputHTMLAttributes, ReactNode } from "react";

type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  hint?: string;
  error?: string;
  icon?: ReactNode;
};

export default function Input({ label, hint, error, icon, ...props }: InputProps) {
  return (
    <label className="field">
      <span className="field__label">{label}</span>
      <div className={`field__control ${error ? "field__control--error" : ""}`.trim()}>
        {icon && <span className="field__icon">{icon}</span>}
        <input {...props} />
      </div>
      {error ? <span className="field__error">{error}</span> : hint && <span className="field__hint">{hint}</span>}
    </label>
  );
}
