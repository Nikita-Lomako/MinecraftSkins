/**
 * @typedef {{
 *   id: string;
 *   skinId: string;
 *   priceUsdFinal: number;
 *   btcUsdRate: number;
 *   purchasedAtUtc: string;
 *   buyerId: string;
 *   skin?: import('../../skin/model/types').SkinPurchaseDto | null;
 * }} Purchase
 */

/**
 * @typedef {{
 *   id: string;
 *   name: string;
 *   basePriceUsd: number;
 *   isAvailable: boolean;
 *   createdAtUtc: string;
 * }} SkinPurchaseDto
 */

/**
 * @typedef {{ skinId: string }} PurchaseCreate
 */
