import { AuthProvider } from 'features/auth';
import { CartProvider } from 'features/cart';

export function AppProviders({ children }) {
    return (
        <AuthProvider>
            <CartProvider>
                {children}
            </CartProvider>
        </AuthProvider>
    );
}