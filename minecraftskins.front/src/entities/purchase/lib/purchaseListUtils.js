/**
 * Клиентская фильтрация, сортировка и пагинация списка покупок (чеков).
 * FSD: entities/purchase/lib
 */

/**
 * @typedef {import('../model/types').Purchase} Purchase
 */

/**
 * Фильтрует список покупок по параметрам (на клиенте).
 * @param {Purchase[]} list
 * @param {{ skinId?: string; from?: string; to?: string; minPrice?: number; maxPrice?: number }} params
 * @returns {Purchase[]}
 */
export function filterPurchases(list, params = {}) {
  let result = list;
  if (params.skinId) {
    result = result.filter((p) => p.skinId === params.skinId);
  }
  if (params.from) {
    const fromTime = new Date(params.from).getTime();
    result = result.filter((p) => new Date(p.purchasedAtUtc).getTime() >= fromTime);
  }
  if (params.to) {
    const toTime = new Date(params.to).getTime();
    result = result.filter((p) => new Date(p.purchasedAtUtc).getTime() <= toTime);
  }
  if (params.minPrice != null && !Number.isNaN(params.minPrice)) {
    result = result.filter((p) => p.priceUsdFinal >= params.minPrice);
  }
  if (params.maxPrice != null && !Number.isNaN(params.maxPrice)) {
    result = result.filter((p) => p.priceUsdFinal <= params.maxPrice);
  }
  return result;
}

/**
 * Сортирует список покупок (на клиенте).
 * @param {Purchase[]} list
 * @param {{ sortBy?: 'Date' | 'Price'; sortOrder?: 'Asc' | 'Desc' }} params
 * @returns {Purchase[]}
 */
export function sortPurchases(list, params = {}) {
  const sortBy = params.sortBy || 'Date';
  const sortOrder = params.sortOrder || 'Desc';
  const asc = sortOrder === 'Asc';
  const sorted = [...list];
  if (sortBy === 'Date') {
    sorted.sort((a, b) => {
      const ta = new Date(a.purchasedAtUtc).getTime();
      const tb = new Date(b.purchasedAtUtc).getTime();
      return asc ? ta - tb : tb - ta;
    });
  } else {
    sorted.sort((a, b) => {
      const diff = a.priceUsdFinal - b.priceUsdFinal;
      return asc ? diff : -diff;
    });
  }
  return sorted;
}

/**
 * Пагинация: возвращает срез списка.
 * @param {Purchase[]} list
 * @param {{ skip?: number; take?: number }} params
 * @returns {Purchase[]}
 */
export function paginatePurchases(list, params = {}) {
  const skip = Math.max(0, params.skip ?? 0);
  const take = Math.max(1, params.take ?? 20);
  return list.slice(skip, skip + take);
}
