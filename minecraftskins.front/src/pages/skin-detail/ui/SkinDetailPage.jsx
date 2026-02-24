import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Container, Card, CardBody, CardTitle, CardFooter, Button } from 'shared/ui';
import { loadSkinDetail } from '../api/skinDetailLoader';
import { BuyButton } from 'features/buy-skin';

function formatPrice(value) {
  if (value == null) return '—';
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
}

function formatDate(iso) {
  return new Date(iso).toLocaleString();
}

export function SkinDetailPage() {
  const { id } = useParams();
  const [skin, setSkin] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [purchaseResult, setPurchaseResult] = useState(null);

  useEffect(() => {
    if (!id) return;
    let cancelled = false;
    loadSkinDetail(id)
      .then((data) => {
        if (!cancelled) setSkin(data);
      })
      .catch((err) => {
        if (!cancelled) setError(err.message);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => { cancelled = true; };
  }, [id]);

  if (loading) {
    return (
      <Container className="py-4">
        <p>Loading…</p>
      </Container>
    );
  }

  if (error || !skin) {
    return (
      <Container className="py-4">
        <div className="alert alert-danger" role="alert">
          {error || 'Skin not found'}
        </div>
        <Link to="/">Back to catalog</Link>
      </Container>
    );
  }

  return (
    <Container className="py-4">
      <p className="mb-3">
        <Link to="/">← Catalog</Link>
      </p>
      <Card>
        <CardBody>
          <CardTitle>{skin.name}</CardTitle>
          <p className="mb-0">
            Base price: {formatPrice(skin.basePriceUsd)} · Final price: {formatPrice(skin.finalPrice ?? skin.basePriceUsd)}
            {skin.currentBtcRate != null && (
              <span className="text-muted small"> (BTC/USD: {Number(skin.currentBtcRate).toFixed(2)})</span>
            )}
          </p>
          <p className="mb-0 small">
            {skin.isAvailable ? (
              <span className="text-success">Available</span>
            ) : (
              <span className="text-secondary">Unavailable</span>
            )}
            {' · Created '}{formatDate(skin.createdAtUtc)}
          </p>
        </CardBody>
        <CardFooter>
          {purchaseResult ? (
            <div className="alert alert-success mb-0" role="alert">
              Purchased: {formatPrice(purchaseResult.priceUsdFinal)} at {formatDate(purchaseResult.purchasedAtUtc)}
            </div>
          ) : skin.isAvailable ? (
            <BuyButton
              skinId={skin.id}
              skinName={skin.name}
              onSuccess={(p) => setPurchaseResult(p)}
              onError={() => setPurchaseResult(null)}
            />
          ) : (
            <Button disabled>Unavailable</Button>
          )}
        </CardFooter>
      </Card>
    </Container>
  );
}
