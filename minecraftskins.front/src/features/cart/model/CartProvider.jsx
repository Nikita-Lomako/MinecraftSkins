import { useCallback, useEffect, useMemo, useState } from 'react';
import { CartContext } from './cartContext';
import { getCart, addCartItem, removeCartItem, clearCart } from 'entities/cart';
import { useAuth } from 'features/auth';

export function CartProvider({ children }) {
    const { token, isAuthenticated } = useAuth();
    const [items, setItems] = useState([]);
    const [totalPrice, setTotalPrice] = useState(0);

    const loadCart = useCallback(async () => {
        if (!token) {
            setItems([]);
            setTotalPrice(0);
            return;
        }
        try {
            const cart = await getCart({ token });
            setItems(cart?.items || []);
            setTotalPrice(cart?.totalPriceUsd || 0);
        } catch {
            setItems([]);
            setTotalPrice(0);
        }
    }, [token]);

    // Эффект теперь только вызывает loadCart, что избавляет от синхронных setState
    useEffect(() => {
        loadCart();
    }, [isAuthenticated, loadCart]);

    const addToCart = useCallback(async (skinId, quantity = 1) => {
        if (!token) return;
        await addCartItem({ skinId, quantity }, { token });
        await loadCart();
    }, [token, loadCart]);

    const removeFromCart = useCallback(async (cartItemId) => {
        if (!token) return;
        await removeCartItem(cartItemId, { token });
        await loadCart();
    }, [token, loadCart]);

    const emptyCart = useCallback(async () => {
        if (!token) return;
        await clearCart({ token });
        await loadCart();
    }, [token, loadCart]);

    const totalQuantity = useMemo(
        () => items.reduce((acc, i) => acc + i.quantity, 0),
        [items]
    );

    const value = useMemo(
        () => ({
            items,
            totalPrice,
            totalQuantity,
            addToCart,
            removeFromCart,
            emptyCart,
            loadCart,
        }),
        [items, totalPrice, totalQuantity, addToCart, removeFromCart, emptyCart, loadCart]
    );

    return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}