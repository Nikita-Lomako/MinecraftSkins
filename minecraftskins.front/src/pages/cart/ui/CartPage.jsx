import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useCart } from 'features/cart';
import { useAuth } from 'features/auth';
import { createPurchase } from 'entities/purchase';

function formatPrice(value) {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
}

const IconTrash = () => (
    <svg className="cart-page__icon-svg" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M3 6h18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
        <path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
        <path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
        <path d="M10 11v6M14 11v6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
    </svg>
);

export function CartPage() {
    const { token } = useAuth();
    const { items, totalPrice, removeFromCart, emptyCart, loadCart } = useCart();
    const [purchasing, setPurchasing] = useState(false);
    const [error, setError] = useState(null);

    const handlePurchaseAll = async () => {
        if (!token || items.length === 0) return;
        setPurchasing(true);
        setError(null);
        try {
            for (const item of items) {
                await createPurchase({ skinId: item.skinId }, { token, idempotencyKey: crypto.randomUUID() });
            }
            await emptyCart();
            await loadCart();
        } catch (err) {
            setError(err.message);
        } finally {
            setPurchasing(false);
        }
    };

    if (!token) {
        return (
            <div className="cart-page">
                <div className="cart-page__container">
                    <p>Please log in to view your cart.</p>
                </div>
            </div>
        );
    }

    return (
        <div className="cart-page">
            <div className="cart-page__container">
                <h1 className="cart-page__title">Shopping Cart</h1>
                {error && <div className="alert alert-danger">{error}</div>}
                {items.length === 0 ? (
                    <div>
                        <p className="cart-page__empty-text">Your cart is empty.</p>
                        <Link className="cart-page__empty-btn" to="/">Browse Skins</Link>
                    </div>
                ) : (
                    <div className="cart-page__grid">
                        <div className="cart-page__lines">
                            {items.map((item) => (
                                <div key={item.id} className="cart-line">
                                    <div className="cart-line__body">
                                        <div className="cart-line__top">
                                            <div>
                                                <span className="cart-line__title">{item.skinName}</span>
                                                <p className="cart-line__category">x{item.quantity}</p>
                                            </div>
                                            <button className="cart-line__remove" onClick={() => removeFromCart(item.id)}><IconTrash /></button>
                                        </div>
                                        <div className="cart-line__bottom">
                                            <div className="cart-line__prices">
                                                <p className="cart-line__line-total">{formatPrice(item.totalPriceUsd)}</p>
                                                <p className="cart-line__each">{formatPrice(item.unitPriceUsd)} each</p>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                        <div className="cart-page__aside">
                            <div className="cart-summary">
                                <h2 className="cart-summary__title">Order Summary</h2>
                                <div className="cart-summary__rows">
                                    <div className="cart-summary__row cart-summary__row--total">
                                        <span className="cart-summary__label">Total</span>
                                        <span className="cart-summary__total">{formatPrice(totalPrice)}</span>
                                    </div>
                                </div>
                                <div className="d-flex gap-2" style={{ marginTop: '1rem' }}>
                                    <button className="cart-summary__checkout" onClick={handlePurchaseAll} disabled={purchasing}>
                                        {purchasing ? 'Purchasing...' : 'Buy All'}
                                    </button>
                                    <button className="cart-summary__checkout" style={{ background: '#6b7280' }} onClick={emptyCart}>
                                        Clear Cart
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}