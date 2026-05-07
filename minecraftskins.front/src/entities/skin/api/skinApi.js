import { apiGet, apiPost, apiPut, apiDelete } from 'shared/api';

/**
 * @param {{ availableOnly?: boolean; search?: string; sortBy?: string; sortOrder?: string; skip?: number; take?: number }} [params]
 * @param {{ token?: string }} [options]
 * @returns {Promise<import('../model/types').Skin[]>}
 */
export async function getSkins(params = {}, options = {}) {
  const q = new URLSearchParams();
  if (params.availableOnly != null) q.set('availableOnly', String(params.availableOnly));
  if (params.search) q.set('search', params.search);
  if (params.sortBy) q.set('sortBy', params.sortBy);
  if (params.sortOrder) q.set('sortOrder', params.sortOrder);
  if (params.skip != null) q.set('skip', String(params.skip));
  if (params.take != null) q.set('take', String(params.take));
  const query = q.toString();
  const path = query ? `skins?${query}` : 'skins';
  return apiGet(path, options);
}

/**
 * @param {string} id - GUID
 * @param {{ token?: string }} [options]
 * @returns {Promise<import('../model/types').Skin | null>}
 */
export async function getSkinById(id, options = {}) {
  return apiGet(`skins/${id}`, options);
}

/**
 * @param {import('../model/types').SkinCreate} dto
 * @param {{ token?: string }} [options]
 * @returns {Promise<import('../model/types').Skin>}
 */
export async function createSkin(dto, options = {}) {
  return apiPost('skins', dto, options);
}

/**
 * @param {string} id - GUID
 * @param {import('../model/types').SkinUpdate} dto
 * @param {{ token?: string }} [options]
 * @returns {Promise<import('../model/types').Skin>}
 */
export async function updateSkin(id, dto, options = {}) {
  return apiPut(`skins/${id}`, dto, options);
}

/**
 * @param {string} id - GUID
 * @param {{ token?: string }} [options]
 */
export async function deleteSkin(id, options = {}) {
  return apiDelete(`skins/${id}`, options);
}
