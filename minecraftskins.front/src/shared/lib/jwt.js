/**
 * Декодирует JWT payload без проверки подписи (только для чтения роли на клиенте).
 * @param {string} token
 * @returns {{ sub?: string; role?: string; [key: string]: unknown } | null}
 */
export function decodeJwtPayload(token) {
  if (!token || typeof token !== 'string') return null;
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(json);
  } catch (_) {
    return null;
  }
}

const ROLE_CLAIMS = [
  'role',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
];

/**
 * @param {string} token
 * @returns {string | null} роль пользователя (например "Admin", "User") или null
 */
export function getRoleFromToken(token) {
  const payload = decodeJwtPayload(token);
  if (!payload) return null;
  for (const key of ROLE_CLAIMS) {
    const value = payload[key];
    if (typeof value === 'string') return value;
    if (Array.isArray(value) && value.length > 0) return value[0];
  }
  return null;
}
