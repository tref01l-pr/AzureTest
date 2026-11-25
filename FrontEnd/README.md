# PdfShare — Frontend

React 18 + Vite 7 + TypeScript (strict), Feature-Sliced Design.

## Run

```bash
npm install
npm run dev          # http://localhost:$WEB_PORT, /api/* -> http://localhost:$API_PORT (prefix stripped)
npm run lint         # ESLint 9 + eslint-plugin-boundaries (FSD layer rules)
npm run build        # tsc -b && vite build
npm run test:run     # vitest + Testing Library + msw
npm run test:e2e     # Playwright; needs the full stack (docker compose up --build) on :3000
```

There is no `.env` in this folder: Vite reads the repo-root `.env` (`envDir: '..'`) for
`WEB_PORT`, `API_PORT` (dev-server proxy target) and `VITE_API_BASE_URL`.

`VITE_API_BASE_URL` defaults to the relative `/api`. Keep it that way:
vite (dev) and nginx (Docker / Azure Container Apps) proxy `/api/*` to the backend, so the
`refreshtoken` cookie is same-origin (`SameSite=Strict`) and no CORS is needed.

## Structure

```
src/app       providers (Query, AuthSession, Router), router, styles
src/pages     login, register, my-files, shared-with-me, share-link (/s/:token), not-found
src/widgets   app-layout, files-table, share-dialog
src/features  auth/{login,register,logout,protected-route}, files/{upload-file,delete-file,download-file},
              share-links/{create-link,revoke-link}, users/search-users
src/entities  user, pdf-file, share-link   (api / model / lib / ui / index.ts)
src/shared    api (apiClient, authTransport, problem-details), config/env, lib, ui
```

Slices are imported only through `index.ts`; a layer may import only layers below it (enforced by lint).

## Auth

- Access token lives in memory only (Zustand, no persistence). On startup
  `bootstrapAuthSession()` calls `POST refresh-access-token` with the HttpOnly cookie; a spinner
  is shown until it resolves.
- `apiClient` adds `Authorization: Bearer`, and on 401 runs a single-flight refresh, then retries
  every queued request. `authTransport` has no interceptors (login/register/refresh).
- If refresh fails: store cleared, `queryClient.clear()`, router navigates to
  `/login?returnUrl=…` (`getSafeReturnUrl` blocks open redirects).
- Errors are branched by ProblemDetails `errors[].code` (e.g. `Auth.InvalidCredentials`).

## Docker

`Dockerfile` builds with Node 20 and serves `dist` from `nginx:alpine`. `nginx.conf.template` is
rendered at container start with `API_UPSTREAM` (default `http://backend:8080`): `/api/` is proxied
to it with the prefix stripped, SPA fallback, gzip, immutable cache for `/assets/`, `no-cache` for
`index.html`. The image does not type-check (`vite build` only): CI runs `npm run build`.
