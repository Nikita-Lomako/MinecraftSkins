import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom';
import { Layout } from '../ui/Layout';
import { RequireAuth } from '../ui/RequireAuth';
import { RequireAdmin } from '../ui/RequireAdmin';
import { CatalogPage } from 'pages/catalog';
import { SkinDetailPage } from 'pages/skin-detail';
import { PurchasesPage } from 'pages/purchases';
import { SignInPage } from 'pages/sign-in';
import { RegisterPage } from 'pages/register';
import { AdminPage, AdminRatePage, AdminSkinsPage } from 'pages/admin';

const router = createBrowserRouter([
  {
    element: <Layout />,
    children: [
      { index: true, element: <CatalogPage /> },
      { path: 'skins/:id', element: <SkinDetailPage /> },
      {
        path: 'purchases',
        element: (
          <RequireAuth>
            <PurchasesPage />
          </RequireAuth>
        ),
      },
      {
        path: 'admin',
        element: (
          <RequireAuth>
            <RequireAdmin>
              <AdminPage />
            </RequireAdmin>
          </RequireAuth>
        ),
      },
      {
        path: 'admin/rate',
        element: (
          <RequireAuth>
            <RequireAdmin>
              <AdminRatePage />
            </RequireAdmin>
          </RequireAuth>
        ),
      },
      {
        path: 'admin/skins',
        element: (
          <RequireAuth>
            <RequireAdmin>
              <AdminSkinsPage />
            </RequireAdmin>
          </RequireAuth>
        ),
      },
      { path: 'login', element: <SignInPage /> },
      { path: 'register', element: <RegisterPage /> },
      { path: '*', element: <Navigate to="/" replace /> },
    ],
  },
]);

export function AppRouter() {
  return <RouterProvider router={router} />;
}
