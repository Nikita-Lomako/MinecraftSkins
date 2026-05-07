import { useState, useEffect, useMemo, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { Container, Row, Col, Card, CardBody, CardTitle, CardFooter, Button } from 'shared/ui';
import { loadCatalog } from '../api/catalogLoader';
import { paginateSkins } from 'entities/skin/lib/skinListUtils';
import { useCart } from 'features/cart';
import { useAuth } from 'features/auth';

function formatPrice(value) {
    if (value == null) return '—';
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
}

const PAGE_SIZE = 10;

export function CatalogPage() {
    const [allSkins, setAllSkins] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [showAll, setShowAll] = useState(false);
    const [page, setPage] = useState(0);
    const [sortBy, setSortBy] = useState('Date');
    const [sortOrder, setSortOrder] = useState('Desc');
    const [search, setSearch] = useState('');
    const { token } = useAuth();
    const { addToCart } = useCart();

    const load = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const list = await loadCatalog({
                availableOnly: !showAll,
                search,
                sortBy,
                sortOrder,
            });
            setAllSkins(list);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }, [showAll, search, sortBy, sortOrder]);

    useEffect(() => {
        load();
    }, [load]);

    const totalSkins = allSkins.length;
    const skins = useMemo(
        () => paginateSkins(allSkins, { skip: page * PAGE_SIZE, take: PAGE_SIZE }),
        [allSkins, page]
    );

    const handleAddToCart = async (skinId) => {
        if (!token) return;
        try {
            await addToCart(skinId, 1);
        } catch (err) {
            // ignore
        }
    };

    return (
        <Container className="py-4">
            <h1 className="mb-4">Minecraft Skins</h1>
            <p className="text-muted mb-3">
                Prices are based on current BTC/USD rate. Add skins to cart to purchase.
            </p>

            <div className="mb-3 d-flex flex-wrap align-items-center gap-3">
                <label className="me-2">
                    <input
                        type="checkbox"
                        checked={showAll}
                        onChange={(e) => {
                            setShowAll(e.target.checked);
                            setPage(0);
                        }}
                    />
                    {' '}Show all (including unavailable)
                </label>
                <span className="d-flex align-items-center gap-2">
                    <input
                        className="form-control form-control-sm"
                        placeholder="Search skins..."
                        value={search}
                        onChange={(e) => { setSearch(e.target.value); setPage(0); }}
                        style={{ width: 220 }}
                    />
                    <label className="small mb-0">Sort by</label>
                    <select
                        className="form-select form-select-sm"
                        style={{ width: 'auto' }}
                        value={sortBy}
                        onChange={(e) => { setSortBy(e.target.value); setPage(0); }}
                    >
                        <option value="Date">Date added</option>
                        <option value="Price">Price</option>
                    </select>
                    <select
                        className="form-select form-select-sm"
                        style={{ width: 'auto' }}
                        value={sortOrder}
                        onChange={(e) => { setSortOrder(e.target.value); setPage(0); }}
                    >
                        <option value="Desc">Desc</option>
                        <option value="Asc">Asc</option>
                    </select>
                </span>
            </div>

            {error && (
                <div className="alert alert-danger" role="alert">
                    {error}
                </div>
            )}

            {loading ? (
                <p>Loading…</p>
            ) : (
                <Row>
                    {skins.length === 0 ? (
                        <Col><p>No skins found.</p></Col>
                    ) : (
                        skins.map((skin) => (
                            <Col key={skin.id} size={12} className="mb-3">
                                <Card>
                                    <CardBody>
                                        <CardTitle>
                                            <Link to={`/skins/${skin.id}`}>{skin.name}</Link>
                                        </CardTitle>
                                        <p className="mb-0">
                                            Base: {formatPrice(skin.basePriceUsd)} · Final: {formatPrice(skin.finalPrice ?? skin.basePriceUsd)}
                                            {skin.currentBtcRate != null && (
                                                <span className="text-muted small"> (rate: {Number(skin.currentBtcRate).toFixed(2)} USD/BTC)</span>
                                            )}
                                        </p>
                                        <p className="mb-0 small">
                                            {skin.isAvailable ? (
                                                <span className="text-success">Available</span>
                                            ) : (
                                                <span className="text-secondary">Unavailable</span>
                                            )}
                                        </p>
                                    </CardBody>
                                    <CardFooter>
                                        {skin.isAvailable ? (
                                            <Button variant="primary" onClick={() => handleAddToCart(skin.id)}>
                                                Add to cart
                                            </Button>
                                        ) : (
                                            <Button disabled>Unavailable</Button>
                                        )}
                                    </CardFooter>
                                </Card>
                            </Col>
                        ))
                    )}
                </Row>
            )}

            <div className="d-flex gap-2 mt-3">
                <Button variant="secondary" disabled={page === 0} onClick={() => setPage((p) => p - 1)}>
                    Previous
                </Button>
                <Button variant="secondary" disabled={skins.length < PAGE_SIZE || (page + 1) * PAGE_SIZE >= totalSkins} onClick={() => setPage((p) => p + 1)}>
                    Next
                </Button>
            </div>
        </Container>
    );
}