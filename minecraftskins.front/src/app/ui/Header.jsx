import { Link } from 'react-router-dom';
import { useAuth } from 'features/auth';
import { useCart } from 'features/cart';

export function Header() {
    const { isAuthenticated, user, isAdmin, signOut } = useAuth();
    const { totalQuantity } = useCart();

    return (
        <header className="site-header">
            <div className="site-header__container">
                <div className="site-header__row">
                    <Link className="site-header__brand" to="/">
                        <div className="site-header__logo-mark" aria-hidden="true">
                            <span>M</span>
                        </div>
                        <span className="site-header__brand-text">Minecraft Skins</span>
                    </Link>

                    <nav className="site-header__nav">
                        <Link className="site-header__nav-link" to="/">Catalog</Link>
                        {isAdmin && <Link className="site-header__nav-link" to="/admin">Admin</Link>}
                        {isAuthenticated && <Link className="site-header__nav-link" to="/purchases">My Purchases</Link>}
                    </nav>

                    <div className="site-header__actions">
                        {isAuthenticated ? (
                            <>
                                <Link className="site-header__icon-btn site-header__cart-link" to="/cart" aria-label="Shopping cart">
                                    <svg className="site-header__icon-svg" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                                        <circle cx="8" cy="21" r="1" stroke="currentColor" strokeWidth="2" />
                                        <circle cx="19" cy="21" r="1" stroke="currentColor" strokeWidth="2" />
                                        <path d="M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                                    </svg>
                                    {totalQuantity > 0 && <span className="site-header__cart-badge">{totalQuantity}</span>}
                                </Link>
                                <span className="site-header__nav-link" style={{ color: '#374151', fontWeight: 500 }}>
                                    {user?.name}
                                </span>
                                <button type="button" className="btn btn-primary btn-sm" onClick={signOut}>
                                    Sign Out
                                </button>
                            </>
                        ) : (
                            <>
                                <Link className="btn btn-primary btn-sm" to="/login">Sign In</Link>
                                <Link className="btn btn-secondary btn-sm" to="/register">Register</Link>
                            </>
                        )}
                    </div>
                </div>
            </div>
        </header>
    );
}