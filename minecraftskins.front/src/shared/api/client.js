import { API_BASE_URL } from '../config';

/**
 * @param {string} token - JWT token (optional)
 * @returns {Headers}
 */
function buildHeaders(token, extra = {}) {
  const headers = { 'Content-Type': 'application/json', ...extra };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  return headers;
}

/**
 * @param {string} path - path without leading slash, e.g. 'skins'
 * @param {RequestInit & { token?: string }} [options]
 * @returns {Promise<Response>}
 */
export async function apiRequest(path, options = {}) {
  const { token, headers: extraHeaders, ...fetchOptions } = options;
  const url = path.startsWith('http') ? path : `${API_BASE_URL.replace(/\/$/, '')}/${path.replace(/^\//, '')}`;
  const mergedHeaders = {
    ...buildHeaders(token, extraHeaders || {}),
    ...(fetchOptions.headers && typeof fetchOptions.headers === 'object' ? fetchOptions.headers : {}),
  };
  const res = await fetch(url, {
    ...fetchOptions,
    headers: mergedHeaders,
  });
  return res;
}

/**
 * GET and parse JSON. Throws on non-ok or invalid JSON.
 * @param {string} path
 * @param {{ token?: string }} [options]
 * @returns {Promise<any>}
 */
export async function apiGet(path, options = {}) {
  const res = await apiRequest(path, { ...options, method: 'GET' });
  const text = await res.text();
  if (!res.ok) {
    let detail = text;
    try {
      const json = JSON.parse(text);
      detail = json.detail || json.message || json.title || text;
    } catch (_) {}
    const err = new Error(detail);
    err.status = res.status;
    err.response = res;
    throw err;
  }
  if (!text) return null;
  return JSON.parse(text);
}

/**
 * POST with JSON body.
 * @param {string} path
 * @param {object} body
 * @param {{ token?: string; headers?: Record<string, string> }} [options]
 * @returns {Promise<any>}
 */
export async function apiPost(path, body, options = {}) {
  const res = await apiRequest(path, {
    ...options,
    method: 'POST',
    body: JSON.stringify(body),
  });
  const text = await res.text();
  if (!res.ok) {
    let detail = text;
    try {
      const json = JSON.parse(text);
      detail = json.detail || json.message || json.title || text;
    } catch (_) {}
    const err = new Error(detail);
    err.status = res.status;
    err.response = res;
    throw err;
  }
  if (!text) return null;
  return JSON.parse(text);
}

/**
 * PUT with JSON body.
 * @param {string} path
 * @param {object} body
 * @param {{ token?: string }} [options]
 * @returns {Promise<any>}
 */
export async function apiPut(path, body, options = {}) {
  const res = await apiRequest(path, {
    ...options,
    method: 'PUT',
    body: JSON.stringify(body),
  });
  const text = await res.text();
  if (!res.ok) {
    let detail = text;
    try {
      const json = JSON.parse(text);
      detail = json.detail || json.message || json.title || text;
    } catch (_) {}
    const err = new Error(detail);
    err.status = res.status;
    err.response = res;
    throw err;
  }
  if (!text) return null;
  return JSON.parse(text);
}

/**
 * DELETE request.
 * @param {string} path
 * @param {{ token?: string }} [options]
 * @returns {Promise<void>}
 */
export async function apiDelete(path, options = {}) {
  const res = await apiRequest(path, { ...options, method: 'DELETE' });
  if (!res.ok) {
    const text = await res.text();
    let detail = text;
    try {
      const json = JSON.parse(text);
      detail = json.detail || json.message || json.title || text;
    } catch (_) {}
    const err = new Error(detail);
    err.status = res.status;
    err.response = res;
    throw err;
  }
}
