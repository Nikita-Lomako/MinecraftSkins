import { apiGet, apiPost, apiDelete } from 'shared/api';

export async function getCart(options = {}) {
    return apiGet('cart', options);
}

export async function addCartItem(dto, options = {}) {
    return apiPost('cart/items', dto, options);
}

export async function removeCartItem(cartItemId, options = {}) {
    return apiDelete(`cart/items/${cartItemId}`, options);
}

export async function clearCart(options = {}) {
    return apiDelete('cart', options);
}