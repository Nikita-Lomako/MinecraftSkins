import { Navigate } from 'react-router-dom';
import { useAuth } from 'features/auth';

/**
 * Показывает дочерний контент только для пользователей с ролью Admin. Иначе редирект на главную.
 */
export function RequireAdmin({ children }) {
  const { isAuthenticated, isAdmin } = useAuth();

  if (!isAuthenticated || !isAdmin) {
    return <Navigate to="/" replace />;
  }

  return children;
}
