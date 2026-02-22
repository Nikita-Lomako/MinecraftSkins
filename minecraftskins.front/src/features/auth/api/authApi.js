import { apiPost } from 'shared/api';

/**
 * @param {{ userName: string; password: string }} body
 * @returns {Promise<{ token: string; userName: string }>}
 */
export async function login(body) {
  return apiPost('login', body);
}

/**
 * @param {{ userName: string; password: string }} body
 * @returns {Promise<{ id: string; name: string }>}
 */
export async function register(body) {
  return apiPost('register', body);
}
