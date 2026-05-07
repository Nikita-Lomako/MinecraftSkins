import { useState, useEffect, useMemo, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { loadCatalog } from '../api/catalogLoader';
import { paginateSkins } from 'entities/skin/lib/skinListUtils';
import { getPurchases } from 'entities/purchase';  // ← ДОБАВИЛ
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
    const [purchasedSkinIds, setPurchasedSkinIds] = useState(new Set()); // ← ДОБАВИЛ
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

    // ← ДОБАВИЛ: загружаем покупки пользователя
    useEffect(() => {
        if (!token) return;
        getPurchases({ buyerId: undefined, take: 500 }, { token })
            .then(purchases => {
                setPurchasedSkinIds(new Set(purchases.map(p => p.skinId)));
            })
            .catch(() => { });
    }, [token]);

    const totalSkins = allSkins.length;
    const skins = useMemo(
        () => paginateSkins(allSkins, { skip: page * PAGE_SIZE, take: PAGE_SIZE }),
        [allSkins, page]
    );

    const handleAddToCart = async (skinId) => {
        if (!token) return;
        addToCart(skinId, 1);
    };

    return (
        <div className="catalog-page">
            <div className="catalog-page__container">
                <div className="catalog-page__intro">
                    <h1 className="catalog-page__heading">Minecraft Skins</h1>
                    <p className="catalog-page__subheading">
                        Prices are based on current BTC/USD rate. Add skins to cart to purchase.
                    </p>
                </div>

                <div className="catalog-toolbar">
                    <div className="catalog-toolbar__left">
                        <label className="catalog-toolbar__filter-check">
                            <input
                                type="checkbox"
                                checked={showAll}
                                onChange={(e) => {
                                    setShowAll(e.target.checked);
                                    setPage(0);
                                }}
                            />
                            <span>Show all (including unavailable)</span>
                        </label>
                        <p className="catalog-toolbar__count">
                            {totalSkins} skin{totalSkins === 1 ? '' : 's'}
                        </p>
                    </div>

                    <div className="catalog-toolbar__sort">
                        <input
                            className="catalog-toolbar__search"
                            placeholder="Search skins..."
                            value={search}
                            onChange={(e) => {
                                setSearch(e.target.value);
                                setPage(0);
                            }}
                        />
                        <span className="catalog-toolbar__sort-label">Sort by:</span>

                        {/* ← ИСПРАВИЛ: оба селекта теперь в .catalog-sort */}
                        <div className="catalog-sort">
                            <select
                                className="catalog-sort__select"
                                value={sortBy}
                                onChange={(e) => {
                                    setSortBy(e.target.value);
                                    setPage(0);
                                }}
                            >
                                <option value="Date">Date added</option>
                                <option value="Price">Price</option>
                            </select>
                            <span className="catalog-sort__chevron">
                                <svg viewBox="0 0 24 24" fill="none" width="16" height="16">
                                    <path
                                        d="m6 9 6 6 6-6"
                                        stroke="currentColor"
                                        strokeWidth="2"
                                        strokeLinecap="round"
                                        strokeLinejoin="round"
                                    />
                                </svg>
                            </span>
                        </div>

                        <div className="catalog-sort">
                            <select
                                className="catalog-sort__select"
                                value={sortOrder}
                                onChange={(e) => {
                                    setSortOrder(e.target.value);
                                    setPage(0);
                                }}
                            >
                                <option value="Desc">Desc</option>
                                <option value="Asc">Asc</option>
                            </select>
                        </div>
                    </div>
                </div>

                {error && <div className="alert alert-danger">{error}</div>}

                {loading ? (
                    <p>Loading…</p>
                ) : (
                    <div className="catalog-grid">
                        {skins.length === 0 ? (
                            <p>No skins found.</p>
                        ) : (
                            skins.map((skin) => (
                                <article key={skin.id} className="product-card">
                                    <Link className="product-card__media" to={`/skins/${skin.id}`}>
                                        <div
                                            className="product-card__img"
                                            style={{
                                                background: '#f3f4f6',
                                                display: 'flex',
                                                alignItems: 'center',
                                                justifyContent: 'center',
                                                height: '100%',
                                            }}
                                        >
                                            <span style={{ fontSize: '2rem' }}>🖼️</span>
                                        </div>
                                    </Link>
                                    <div className="product-card__body">
                                        <Link className="product-card__title-link" to={`/skins/${skin.id}`}>
                                            <h3 className="product-card__title">{skin.name}</h3>
                                        </Link>
                                        <div className="product-card__footer">
                                            <span className="product-card__price">
                                                {formatPrice(skin.finalPrice ?? skin.basePriceUsd)}
                                            </span>
                                            <span className="product-card__category">
                                                {skin.isAvailable ? 'Available' : 'Unavail.'}
                                            </span>
                                        </div>

                                        {/* ← ИСПРАВИЛ: кнопка с проверкой на покупку */}
                                        {skin.isAvailable ? (
                                            purchasedSkinIds.has(skin.id) ? (
                                                <button className="product-card__btn" disabled>
                                                    Purchased
                                                </button>
                                            ) : (
                                                <button
                                                    className="product-card__btn"
                                                    onClick={() => handleAddToCart(skin.id)}
                                                >
                                                    Add to cart
                                                </button>
                                            )
                                        ) : (
                                            <button className="product-card__btn" disabled>
                                                Unavailable
                                            </button>
                                        )}
                                    </div>
                                </article>
                            ))
                        )}
                    </div>
                )}

                <div className="d-flex gap-2 mt-3">
                    <button
                        className="btn btn-secondary"
                        disabled={page === 0}
                        onClick={() => setPage((p) => p - 1)}
                    >
                        Previous
                    </button>
                    <button
                        className="btn btn-secondary"
                        disabled={skins.length < PAGE_SIZE || (page + 1) * PAGE_SIZE >= totalSkins}
                        onClick={() => setPage((p) => p + 1)}
                    >
                        Next
                    </button>
                </div>
            </div>
        </div>
    );
}