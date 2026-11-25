import { lazy, Suspense } from 'react'
import { createBrowserRouter, Navigate, Outlet } from 'react-router-dom'
import { GuestOnlyRoute, ProtectedRoute } from '@features/auth/protected-route'
import { FullPageSpinner } from '@shared/ui'
import { AppLayout } from '@widgets/app-layout'
import { RouteError } from './RouteError'

const LoginPage = lazy(() => import('@pages/login').then((m) => ({ default: m.LoginPage })))
const RegisterPage = lazy(() =>
  import('@pages/register').then((m) => ({ default: m.RegisterPage })),
)
const MyFilesPage = lazy(() => import('@pages/my-files').then((m) => ({ default: m.MyFilesPage })))
const SharedWithMePage = lazy(() =>
  import('@pages/shared-with-me').then((m) => ({ default: m.SharedWithMePage })),
)
const ShareLinkPage = lazy(() =>
  import('@pages/share-link').then((m) => ({ default: m.ShareLinkPage })),
)
const NotFoundPage = lazy(() =>
  import('@pages/not-found').then((m) => ({ default: m.NotFoundPage })),
)

export const router = createBrowserRouter(
  [
    {
      element: (
        <Suspense fallback={<FullPageSpinner />}>
          <Outlet />
        </Suspense>
      ),
      errorElement: <RouteError />,
      children: [
        { path: '/', element: <Navigate to="/files" replace /> },
        {
          element: <GuestOnlyRoute />,
          children: [
            { path: '/login', element: <LoginPage /> },
            { path: '/register', element: <RegisterPage /> },
          ],
        },
        { path: '/s/:token', element: <ShareLinkPage /> },
        {
          element: <ProtectedRoute />,
          children: [
            {
              element: <AppLayout />,
              errorElement: <RouteError />,
              children: [
                { path: '/files', element: <MyFilesPage /> },
                { path: '/shared', element: <SharedWithMePage /> },
              ],
            },
          ],
        },
        { path: '*', element: <NotFoundPage /> },
      ],
    },
  ],
  {
    future: {
      v7_relativeSplatPath: true,
      v7_fetcherPersist: true,
      v7_normalizeFormMethod: true,
      v7_partialHydration: true,
      v7_skipActionErrorRevalidation: true,
    },
  },
)
