/**
 * Валидация создания скина на клиенте (FSD: entities/skin/model).
 * Соответствует правилам бэкенда: Name не пустое, до 100 символов; BasePriceUsd > 0 и <= 999999.99; IsAvailable обязательно.
 */

/**
 * @param {{ name?: string; basePriceUsd?: number; isAvailable?: boolean }} data
 * @returns {{ valid: boolean; errors: string[] }}
 */
export function validateSkinCreate(data) {
  const errors = [];
  const name = data?.name?.trim();
  if (name === undefined || name === '') {
    errors.push('Name is required');
  } else if (name.length > 100) {
    errors.push('Name must be at most 100 characters');
  }
  const price = data?.basePriceUsd;
  if (price === undefined || price === null) {
    errors.push('Base price is required');
  } else {
    const n = Number(price);
    if (Number.isNaN(n)) {
      errors.push('Base price must be a number');
    } else if (n <= 0) {
      errors.push('Base price must be greater than 0');
    } else if (n > 9999.99) {
      errors.push('Base price must be at most 9999.99');
    }
  }
  if (data?.isAvailable === undefined || data?.isAvailable === null) {
    errors.push('Availability (isAvailable) is required');
  }
  return {
    valid: errors.length === 0,
    errors,
  };
}
