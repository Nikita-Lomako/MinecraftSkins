import { getSkins } from 'entities/skin';

/**
 * Загружает скины для каталога (сервер отдаёт список без сортировки).
 * Сортировка и пагинация выполняются на клиенте.
 * @param {{ availableOnly?: boolean; search?: string }} [params]
 * @returns {Promise<import('entities/skin').Skin[]>}
 */
export async function loadCatalog(params = {}) {
  const availableOnly = params.availableOnly ?? true;
  const search = params.search;
  const sortBy = params.sortBy ?? 'Date';
  const sortOrder = params.sortOrder ?? 'Desc';
  return getSkins({
    availableOnly,
    search,
    sortBy,
    sortOrder,
    skip: 0,
    take: 100,
  });
}
