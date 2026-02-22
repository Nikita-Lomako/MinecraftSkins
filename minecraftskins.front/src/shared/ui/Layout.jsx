export function Container({ children, className = '' }) {
  return <div className={`container ${className}`}>{children}</div>;
}

export function Row({ children, className = '' }) {
  return <div className={`row ${className}`}>{children}</div>;
}

export function Col({ children, size = 12, className = '' }) {
  return <div className={`col-md-${size} ${className}`}>{children}</div>;
}
