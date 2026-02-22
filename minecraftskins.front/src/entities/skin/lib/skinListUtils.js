/**
 * Клиентская сортировка и пагинация списка скинов.
 * FSD: entities/skin/lib
 */

/**
 * @typedef {import('../model/types').Skin} Skin
 */

/**
 * Сортирует список скинов по цене или дате добавления (на клиенте).
 * @param {Skin[]} list
 * @param {{ sortBy?: 'Price' | 'Date'; sortOrder?: 'Asc' | 'Desc' }} params
 * @returns {Skin[]}
 */
export function sortSkins(list, params = {}) {
  const sortBy = params.sortBy || 'Date';
  const sortOrder = params.sortOrder || 'Desc';
  const asc = sortOrder === 'Asc';
  const sorted = [...list];
  if (sortBy === 'Price') {
    sorted.sort((a, b) => {
      const diff = (a.basePriceUsd ?? 0) - (b.basePriceUsd ?? 0);
      return asc ? diff : -diff;
    });
  } else {
    sorted.sort((a, b) => {
      const ta = new Date(a.createdAtUtc || 0).getTime();
      const tb = new Date(b.createdAtUtc || 0).getTime();
      return asc ? ta - tb : tb - ta;
    });
  }
  return sorted;
}

/**
 * Пагинация: возвращает срез списка.
 * @param {Skin[]} list
 * @param {{ skip?: number; take?: number }} params
 * @returns {Skin[]}
 */
export function paginateSkins(list, params = {}) {
  const skip = Math.max(0, params.skip ?? 0);
  const take = Math.max(1, params.take ?? 10);
  return list.slice(skip, skip + take);
}
