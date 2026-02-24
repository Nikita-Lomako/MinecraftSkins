import { createContext, useContext, useState, useCallback } from 'react';
import { getRoleFromToken } from 'shared/lib/jwt';

const AuthContext = createContext(null);

const TOKEN_KEY = 'mcskins_token';
const USER_KEY = 'mcskins_user';

function loadStored() {
  try {
    const token = localStorage.getItem(TOKEN_KEY);
    const userJson = localStorage.getItem(USER_KEY);
    if (token && userJson) {
      const user = JSON.parse(userJson);
      return { token, user: { id: user.id, name: user.name } };
    }
  } catch (_) {}
  return null;
}

export function AuthProvider({ children }) {
  const [state, setState] = useState(loadStored);

  const signIn = useCallback((token, user) => {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    setState({ token, user: { id: user.id, name: user.name } });
  }, []);

  const signOut = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    setState(null);
  }, []);

  const isAdmin = state ? getRoleFromToken(state.token) === 'Admin' : false;

  const value = state
    ? {
        isAuthenticated: true,
        token: state.token,
        user: state.user,
        isAdmin,
        signIn,
        signOut,
      }
    : {
        isAuthenticated: false,
        token: null,
        user: null,
        isAdmin: false,
        signIn,
        signOut,
      };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
