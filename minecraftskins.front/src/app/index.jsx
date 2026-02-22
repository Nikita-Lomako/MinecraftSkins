import { StrictMode } from 'react';
import './styles/index.css';
import { AppProviders } from './providers/AppProviders';
import { AppRouter } from './routes';

export function App() {
  return (
    <StrictMode>
      <AppProviders>
        <AppRouter />
      </AppProviders>
    </StrictMode>
  );
}
