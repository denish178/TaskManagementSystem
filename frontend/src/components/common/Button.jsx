const variantClass = {
  primary: "btn btn-primary",
  secondary: "btn btn-secondary",
  ghost: "btn btn-ghost",
  danger: "btn btn-danger",
};

export default function Button({
  children,
  variant = "primary",
  type = "button",
  className = "",
  ...props
}) {
  return (
    <button
      type={type}
      className={`${variantClass[variant] ?? variantClass.primary} ${className}`.trim()}
      {...props}
    >
      {children}
    </button>
  );
}
