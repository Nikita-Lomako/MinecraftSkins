import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Container, Card, CardBody, CardTitle } from 'shared/ui';
import { getBtcUsdRate } from 'entities/rate';
import { useAuth } from 'features/auth';

function formatDate(iso) {
  return new Date(iso).toLocaleString();
}

export function AdminRatePage() {
  const { token } = useAuth();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (!token) return;
    getBtcUsdRate({ token })
      .then(setData)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [token]);

  return (
    <Container className="py-4">
      <p className="mb-3">
        <Link to="/admin">← Admin</Link>
      </p>
      <h1 className="mb-4">BTC/USD rate</h1>
      {error && (
        <div className="alert alert-danger">{error}</div>
      )}
      {loading && <p>Loading…</p>}
      {data && (
        <Card>
          <CardBody>
            <CardTitle>{Number(data.rate).toFixed(2)} USD/BTC</CardTitle>
            <p className="mb-0">Source: {data.source}</p>
            <p className="mb-0 small text-muted">
              As of: {formatDate(data.asOfUtc)}
              {data.ageSeconds != null && ` · age: ${data.ageSeconds} s`}
            </p>
          </CardBody>
        </Card>
      )}
    </Container>
  );
}
