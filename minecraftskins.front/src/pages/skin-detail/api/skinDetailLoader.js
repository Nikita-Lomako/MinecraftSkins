import { getSkinById } from 'entities/skin';

/**
 * @param {string} id - Skin GUID
 */
export async function loadSkinDetail(id) {
  return getSkinById(id);
}
