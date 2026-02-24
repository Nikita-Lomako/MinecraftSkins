export function Button({ children, variant = 'primary', type = 'button', disabled, className = '', ...props }) {
  const base = 'btn';
  const variants = {
    primary: 'btn-primary',
    secondary: 'btn-secondary',
    danger: 'btn-danger',
    outline: 'btn-outline-primary',
  };
  const cls = [base, variants[variant] || variants.primary, className].filter(Boolean).join(' ');
  return (
    <button type={type} className={cls} disabled={disabled} {...props}>
      {children}
    </button>
  );
}
