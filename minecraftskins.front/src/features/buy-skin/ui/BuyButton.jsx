import { useState } from 'react';
import { Button } from 'shared/ui';
import { createPurchase } from 'entities/purchase';
import { useAuth } from 'features/auth';

/**
 * @param {{ skinId: string; skinName: string; onSuccess?: (purchase: import('entities/purchase').Purchase) => void; onError?: (err: Error) => void }}
 */
export function BuyButton({ skinId, skinName, onSuccess, onError }) {
  const { token } = useAuth();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const handleBuy = async () => {
    if (!token) {
      onError?.(new Error('You must be logged in to buy'));
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const purchase = await createPurchase(
        { skinId },
        { token, idempotencyKey: crypto.randomUUID() }
      );
      onSuccess?.(purchase);
    } catch (err) {
      setError(err.message);
      onError?.(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <Button
        variant="primary"
        disabled={loading}
        onClick={handleBuy}
      >
        {loading ? 'Buying…' : `Buy ${skinName}`}
      </Button>
      {error && <p className="text-danger small mt-1">{error}</p>}
    </div>
  );
}
