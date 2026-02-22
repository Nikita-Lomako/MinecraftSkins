import { getPurchases } from 'entities/purchase';

/**
 * Загружает все покупки текущего пользователя (без фильтров на сервере).
 * Фильтрация, сортировка и пагинация выполняются на клиенте.
 * @param {{ token: string }} options
 * @returns {Promise<import('entities/purchase').Purchase[]>}
 */
export async function loadPurchases(options) {
  const { token } = options;
  return getPurchases({ skip: 0, take: 5000 }, { token });
}
