import { apiGet } from 'shared/api';

/**
 * GET /api/rates/btc-usd (требует роль Admin)
 * @param {{ token: string }} options
 * @returns {Promise<{ rate: number; asOfUtc: string; source: string; ageSeconds?: number }>}
 */
export async function getBtcUsdRate(options) {
  return apiGet('rates/btc-usd', options);
}
