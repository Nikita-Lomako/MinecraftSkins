import { useState, useEffect, useMemo } from 'react';
import { Container, Row, Col, Card, CardBody, CardTitle, Button, Input } from 'shared/ui';
import { useAuth } from 'features/auth';
import { getSkins } from 'entities/skin';
import { filterPurchases, sortPurchases, paginatePurchases } from 'entities/purchase/lib/purchaseListUtils';
import { loadPurchases } from '../api/purchasesLoader';

function formatPrice(value) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
}

function formatDate(iso) {
  return new Date(iso).toLocaleString();
}

const PAGE_SIZE = 20;

export function PurchasesPage() {
  const { token } = useAuth();
  const [allPurchases, setAllPurchases] = useState([]);
  const [skins, setSkins] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [page, setPage] = useState(0);
  const [filters, setFilters] = useState({
    skinId: '',
    from: '',
    to: '',
    minPrice: '',
    maxPrice: '',
    sortBy: 'Date',
    sortOrder: 'Desc',
  });

  useEffect(() => {
    if (!token) {
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    loadPurchases({ token })
      .then(setAllPurchases)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [token]);

  useEffect(() => {
    getSkins({ availableOnly: false, take: 500 })
      .then(setSkins)
      .catch(() => setSkins([]));
  }, []);

  const filtered = useMemo(() => {
    const from = filters.from ? new Date(filters.from).toISOString() : undefined;
    const to = filters.to ? new Date(filters.to).toISOString() : undefined;
    const minPrice = filters.minPrice !== '' ? Number(filters.minPrice) : undefined;
    const maxPrice = filters.maxPrice !== '' ? Number(filters.maxPrice) : undefined;
    let list = filterPurchases(allPurchases, {
      skinId: filters.skinId || undefined,
      from,
      to,
      minPrice: minPrice !== undefined && !Number.isNaN(minPrice) ? minPrice : undefined,
      maxPrice: maxPrice !== undefined && !Number.isNaN(maxPrice) ? maxPrice : undefined,
    });
    list = sortPurchases(list, { sortBy: filters.sortBy, sortOrder: filters.sortOrder });
    return list;
  }, [allPurchases, filters]);

  const totalFiltered = filtered.length;
  const purchases = useMemo(
    () => paginatePurchases(filtered, { skip: page * PAGE_SIZE, take: PAGE_SIZE }),
    [filtered, page]
  );

  const updateFilter = (key, value) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
    setPage(0);
  };

  if (!token) {
    return (
      <Container className="py-4">
        <p>Please log in to view your purchases.</p>
      </Container>
    );
  }

  return (
    <Container className="py-4">
      <h1 className="mb-4">My Purchases</h1>

      <Card className="mb-4">
        <CardBody>
          <CardTitle className="h6">Filters &amp; sort (client-side)</CardTitle>
          <div className="row g-2 align-items-end">
            <Col size={12} md={6} lg={2}>
              <label className="form-label small">Skin</label>
              <select
                className="form-select form-select-sm"
                value={filters.skinId}
                onChange={(e) => updateFilter('skinId', e.target.value)}
              >
                <option value="">All</option>
                {skins.map((s) => (
                  <option key={s.id} value={s.id}>{s.name}</option>
                ))}
              </select>
            </Col>
            <Col size={6} md={3} lg={2}>
              <Input
                label="From date"
                type="date"
                value={filters.from}
                onChange={(e) => updateFilter('from', e.target.value)}
              />
            </Col>
            <Col size={6} md={3} lg={2}>
              <Input
                label="To date"
                type="date"
                value={filters.to}
                onChange={(e) => updateFilter('to', e.target.value)}
              />
            </Col>
            <Col size={6} md={3} lg={1}>
              <Input
                label="Min $"
                type="number"
                step="0.01"
                min="0"
                value={filters.minPrice}
                onChange={(e) => updateFilter('minPrice', e.target.value)}
              />
            </Col>
            <Col size={6} md={3} lg={1}>
              <Input
                label="Max $"
                type="number"
                step="0.01"
                min="0"
                value={filters.maxPrice}
                onChange={(e) => updateFilter('maxPrice', e.target.value)}
              />
            </Col>
            <Col size={6} md={3} lg={2}>
              <label className="form-label small">Sort by</label>
              <select
                className="form-select form-select-sm"
                value={filters.sortBy}
                onChange={(e) => updateFilter('sortBy', e.target.value)}
              >
                <option value="Date">Date</option>
                <option value="Price">Price</option>
              </select>
            </Col>
            <Col size={6} md={3} lg={2}>
              <label className="form-label small">Order</label>
              <select
                className="form-select form-select-sm"
                value={filters.sortOrder}
                onChange={(e) => updateFilter('sortOrder', e.target.value)}
              >
                <option value="Desc">Desc</option>
                <option value="Asc">Asc</option>
              </select>
            </Col>
          </div>
          <p className="small text-muted mb-0 mt-2">{totalFiltered} receipt(s)</p>
        </CardBody>
      </Card>

      {error && (
        <div className="alert alert-danger">{error}</div>
      )}

      {loading ? (
        <p>Loading purchases…</p>
      ) : (
        <>
          {purchases.length === 0 ? (
            <p>No purchases match the filters.</p>
          ) : (
            <Row>
              {purchases.map((p) => (
                <Col key={p.id} size={12} className="mb-3">
                  <Card>
                    <CardBody>
                      <CardTitle>Purchase {p.id.slice(0, 8)}…</CardTitle>
                      <p className="mb-0">
                        <strong>{formatPrice(p.priceUsdFinal)}</strong> (rate: {Number(p.btcUsdRate).toFixed(2)} USD/BTC)
                      </p>
                      <p className="mb-0 small text-muted">
                        {p.skin ? `Skin: ${p.skin.name}` : `Skin ID: ${p.skinId}`} · {formatDate(p.purchasedAtUtc)}
                      </p>
                    </CardBody>
                  </Card>
                </Col>
              ))}
            </Row>
          )}

          <div className="d-flex gap-2 mt-3">
            <Button
              variant="secondary"
              disabled={page === 0}
              onClick={() => setPage((p) => p - 1)}
            >
              Previous
            </Button>
            <Button
              variant="secondary"
              disabled={(page + 1) * PAGE_SIZE >= totalFiltered}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </Button>
          </div>
        </>
      )}
    </Container>
  );
}
