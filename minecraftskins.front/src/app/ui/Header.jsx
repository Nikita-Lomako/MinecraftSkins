import { Link } from 'react-router-dom';
import { useAuth } from 'features/auth';
import { useCart } from 'features/cart';

export function Header() {
    const { isAuthenticated, user, isAdmin, signOut } = useAuth();
    const { totalQuantity } = useCart();

    return (
        <nav className="navbar navbar-expand-lg navbar-dark bg-dark mb-4">
            <div className="container">
                <Link className="navbar-brand" to="/">
                    Minecraft Skins
                </Link>
                <div className="navbar-nav ms-auto">
                    <Link className="nav-link" to="/">
                        Catalog
                    </Link>
                    {isAdmin && (
                        <Link className="nav-link" to="/admin">
                            Admin
                        </Link>
                    )}
                    {isAuthenticated ? (
                        <>
                            <Link className="nav-link" to="/purchases">
                                My Purchases
                            </Link>
                            <Link className="nav-link position-relative" to="/cart">
                                🛒 Cart
                                {totalQuantity > 0 && (
                                    <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">
                                        {totalQuantity}
                                    </span>
                                )}
                            </Link>
                            <span className="navbar-text me-2">{user?.name}</span>
                            <button type="button" className="btn btn-outline-light btn-sm" onClick={signOut}>
                                Sign Out
                            </button>
                        </>
                    ) : (
                        <>
                            <Link className="nav-link" to="/login">
                                Sign In
                            </Link>
                            <Link className="nav-link" to="/register">
                                Register
                            </Link>
                        </>
                    )}
                </div>
            </div>
        </nav>
    );
}