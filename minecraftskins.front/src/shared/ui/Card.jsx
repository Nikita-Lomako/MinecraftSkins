export function Card({ children, className = '' }) {
  return <div className={`card ${className}`}>{children}</div>;
}

export function CardBody({ children, className = '' }) {
  return <div className={`card-body ${className}`}>{children}</div>;
}

export function CardTitle({ children, className = '' }) {
  return <h5 className={`card-title ${className}`}>{children}</h5>;
}

export function CardFooter({ children, className = '' }) {
  return <div className={`card-footer ${className}`}>{children}</div>;
}
