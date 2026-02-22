import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from 'features/auth';

/**
 * Wraps a route that requires authentication. Redirects to /login if not signed in.
 */
export function RequireAuth({ children }) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return children;
}
