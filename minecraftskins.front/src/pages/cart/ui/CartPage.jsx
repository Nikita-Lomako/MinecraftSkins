import { useEffect, useState } from 'react';
import { Container, Button } from 'shared/ui';
import { useAuth } from 'features/auth';
import { getCart, removeCartItem, clearCart } from 'entities/cart';

export function CartPage() {
  const { token } = useAuth();
  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const load = async () => {
    if (!token) return;
    setLoading(true);
    try {
      const data = await getCart({ token });
      setCart(data);
      setError(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [token]);

  if (!token) {
    return <Container className="py-4">Please log in to use cart.</Container>;
  }

  return (
    <Container className="py-4">
      <h1>My Cart</h1>
      {error && <div className="alert alert-danger">{error}</div>}
      {loading ? <p>Loading...</p> : (
        <>
          {(!cart?.items || cart.items.length === 0) ? <p>Cart is empty.</p> : (
            <div className="list-group mb-3">
              {cart.items.map((item) => (
                <div key={item.id} className="list-group-item d-flex justify-content-between align-items-center">
                  <div>{item.skinName} x {item.quantity}</div>
                  <div className="d-flex align-items-center gap-2">
                    <span>${Number(item.totalPriceUsd).toFixed(2)}</span>
                    <Button variant="danger" onClick={async () => { await removeCartItem(item.id, { token }); await load(); }}>
                      Remove
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
          <p><strong>Total:</strong> ${Number(cart?.totalPriceUsd ?? 0).toFixed(2)}</p>
          <Button variant="secondary" onClick={async () => { await clearCart({ token }); await load(); }}>
            Clear cart
          </Button>
        </>
      )}
    </Container>
  );
}
