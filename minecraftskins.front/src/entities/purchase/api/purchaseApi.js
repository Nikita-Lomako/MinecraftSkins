import { apiGet, apiPost } from 'shared/api';

/**
 * @param {{ buyerId?: string; skinId?: string; from?: string; to?: string; skip?: number; take?: number }} [params]
 * @param {{ token: string }} options - token required
 * @returns {Promise<import('../model/types').Purchase[]>}
 */
export async function getPurchases(params = {}, options = {}) {
  const q = new URLSearchParams();
  if (params.buyerId) q.set('buyerId', params.buyerId);
  if (params.skinId) q.set('skinId', params.skinId);
  if (params.from) q.set('from', params.from);
  if (params.to) q.set('to', params.to);
  if (params.skip != null) q.set('skip', String(params.skip));
  if (params.take != null) q.set('take', String(params.take));
  const query = q.toString();
  const path = query ? `purchases?${query}` : 'purchases';
  return apiGet(path, options);
}

/**
 * @param {string} id - GUID
 * @param {{ token: string }} options
 * @returns {Promise<import('../model/types').Purchase>}
 */
export async function getPurchaseById(id, options) {
  return apiGet(`purchases/${id}`, options);
}

/**
 * @param {import('../model/types').PurchaseCreate} dto
 * @param {{ token: string; idempotencyKey?: string }} options
 * @returns {Promise<import('../model/types').Purchase>}
 */
export async function createPurchase(dto, options = {}) {
  const { token, idempotencyKey = crypto.randomUUID() } = options;
  const headers = { 'Idempotency-Key': idempotencyKey };
  return apiPost('purchases', dto, { token, headers });
}
