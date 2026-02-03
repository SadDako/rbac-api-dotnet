type SpinnerProps = {
  size?: "sm" | "md" | "lg";
  className?: string;
};

export default function Spinner({ size = "md", className = "" }: SpinnerProps) {
  return <span className={`spinner spinner--${size} ${className}`.trim()} aria-hidden="true" />;
}
