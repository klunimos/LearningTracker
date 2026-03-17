---
name: new-angular-project
description: Full setup guide for creating a new Angular project in this workspace. Covers ng new cleanup, tsconfig consolidation, spec file removal, and standard infrastructure (environments, token storage, auth guard, interceptor, app.config.ts). Use when creating a new Angular application or setting up a fresh Angular project.
---

# New Angular Project – Setup Guide

## Step 1 – Repository Environment Files

### `.gitignore` (repository root)

```
## Visual Studio
.vs/
*.user
*.suo

## Build outputs
bin/
obj/
*.dll
*.exe
*.pdb

## .NET
project.lock.json
project.fragment.lock.json
artifacts/
TestResults/

## IDE
.vscode/*
!.vscode/settings.json
.idea/

## Node / Frontend
node_modules/
dist/
```

### `.vscode/settings.json` (repository root)

```json
{
  "files.exclude": {
    "**/TestResults": true,
    "**/.vs": true,
    "**/dist": true,
    "**/node_modules": true,
    "**/*.sln": true
  }
}
```

### `.vscode/settings.json` (inside Angular folder)

```json
{
  "files.exclude": {
    "**/TestResults": true,
    "**/.vs": true,
    "**/dist": true,
    "**/node_modules": true,
    "**/*.sln": true,
    "**/.angular": true
  }
}
```

This file is tracked in Git (exception in `.gitignore`) so the team shares the same Explorer view.

---

## Step 2 – Spec Files Cleanup (after `ng new`)

1. Delete `README.md`
2. Delete `tsconfig.spec.json`
3. Remove the `tsconfig.spec.json` reference from `tsconfig.json`
4. Remove the `test` section from `angular.json`
5. Delete any `.spec.ts` files created by `ng new`
6. Merge `tsconfig.app.json` into `tsconfig.json` (add `include`, `outDir`, `types`; remove `references`) and delete `tsconfig.app.json`
7. Update `angular.json` to use `tsConfig: "tsconfig.json"` instead of `tsconfig.app.json`

In `angular.json` schematics – prevent future generation by adding `"skipTests": true` to all schematics (component, service, directive, pipe, guard, interceptor, resolver).

---

## Step 2 – TypeScript Config

Use one `tsconfig.json`. No separate `tsconfig.app.json` or `tsconfig.spec.json`.

- Merge all TypeScript config into `tsconfig.json` (compiler options, include, outDir)
- `angular.json` references `tsConfig: "tsconfig.json"`

---

## Step 3 – Routes

Use `--routing` flag with `ng new`, then immediately merge `app.routes.ts` into `app.config.ts` and delete the separate routes file. Do not keep standalone `*.routes.ts` files.

When adding new routes later — always add them to the `routes` array in `app.config.ts`. Never create `app-routing.module.ts` or separate `*.routes.ts` files.

---

## Step 4 – Tailwind v4 Source Detection

Create `src/tailwind.css` with explicit source detection:

```css
@import "tailwindcss" source("../src");
@import "tailwindcss-primeui";
@source "./**/*.{html,ts}";
```

Without this, many utility classes won't be generated and pages may look unstyled. Restart `ng serve` after changing this file.

Configure the primary color palette once in `app.config.ts`:

```typescript
import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

providePrimeNG({
  theme: {
    preset: definePreset(Aura, {
      semantic: {
        primary: {
          50:  '#eef4fb', 100: '#d9e8f7', 200: '#b3d1ef',
          300: '#80b2e3', 400: '#5090d6', 500: '#3474cc',
          600: '#2860b0', 700: '#1f4e94', 800: '#183f78',
          900: '#133260', 950: '#0a1d3a',
        },
      },
    }),
  },
})
```

---

## Step 6 – Standard Infrastructure

Create the following files before any feature work begins:

```
src/
  environments/
    environment.ts             ← production values
    environment.development.ts ← local dev values (committed to Git)
  app/
    models/
      result-data.model.ts     ← ResultData<T> wrapper
    services/
      token-storage.service.ts ← LS / SS token management
    guards/
      auth.guard.ts            ← protects authenticated routes
    interceptors/
      auth.interceptor.ts      ← attaches token + handles failures globally
```

### Environment Files

Run `ng generate environments` — creates files and updates `angular.json` `fileReplacements`.

```typescript
// environment.ts (production)
export const environment = {
  production: true,
  apiUrl: 'https://your-production-server.com',
  googleClientId: 'YOUR_PROD_CLIENT_ID'
};

// environment.development.ts (local dev)
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  googleClientId: 'YOUR_DEV_CLIENT_ID'
};
```

Both files are committed to Git — `apiUrl` and `googleClientId` are not secrets.
All services use `environment.apiUrl` as the base URL. Never hardcode URLs or keys directly in code.

---

### `result-data.model.ts`

```typescript
export interface ResultData<T> {
  success: boolean;
  message: string | null;
  value: T | null;
}
```

---

### `token-storage.service.ts`

```typescript
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly REFRESH_KEY = 'refresh_token';

  saveToken(token: string, rememberMe: boolean): void {
    const storage = rememberMe ? localStorage : sessionStorage;
    storage.setItem(this.TOKEN_KEY, token);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY) ?? sessionStorage.getItem(this.TOKEN_KEY);
  }

  saveRefreshToken(token: string): void {
    localStorage.setItem(this.REFRESH_KEY, token);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_KEY);
  }

  getTokenPayload(): TokenPayload | null {
    const token = this.getToken();
    if (!token) return null;
    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload as TokenPayload;
  }

  clearAll(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_KEY);
    sessionStorage.removeItem(this.TOKEN_KEY);
  }
}
```

`TokenPayload` — define in `models/token-payload.model.ts`, match the JWT claims from the server:

```typescript
export interface TokenPayload {
  sub: number;    // userId
  exp: number;    // expiry (Unix timestamp)
  // Add project-specific claim fields as needed
}
```

For project-specific persistent state (e.g. last selected context ID), add methods to the same service using generic names (`saveContextId`, `getContextId`) and clear them in `clearAll()`.

---

### `auth.guard.ts`

The guard checks token **existence only**. Token validity and refresh are the interceptor's responsibility. Do not check token expiry in the guard — expired tokens are handled transparently by the interceptor.

```typescript
export const authGuard: CanActivateFn = () => {
  const tokenStorage = inject(TokenStorageService);
  const router = inject(Router);
  if (tokenStorage.getToken()) return true;
  return router.createUrlTree(['/login']);
};
```

---

### `auth.interceptor.ts`

Two separate paths:
- **`success: false` in body** (HTTP 200) → business error → show toast
- **HTTP 401** → token expired → try refresh; if no refresh or refresh fails → logout

```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenStorage = inject(TokenStorageService);
  const messageService = inject(MessageService);
  const router = inject(Router);
  const http = inject(HttpClient);

  const token = tokenStorage.getToken();
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    switchMap(event => {
      if (event instanceof HttpResponse && event.body?.success === false) {
        messageService.add({ severity: 'error', summary: 'שגיאה', detail: event.body.message });
        return EMPTY;
      }
      return of(event);
    }),
    catchError(err => {
      if (err.status !== 401) return EMPTY;

      const refreshToken = tokenStorage.getRefreshToken();
      if (!refreshToken) {
        tokenStorage.clearAll();
        router.navigate(['/login']);
        return EMPTY;
      }

      const refreshUrl = `${environment.apiUrl}/Auth/Refresh`;
      return http.post<ResultData<TokenData>>(refreshUrl, { refreshToken }).pipe(
        switchMap(result => {
          if (!result.success) {
            tokenStorage.clearAll();
            router.navigate(['/login']);
            return EMPTY;
          }
          tokenStorage.saveToken(result.value!.token, true);
          if (result.value!.refreshToken)
            tokenStorage.saveRefreshToken(result.value!.refreshToken);
          const retryReq = req.clone({ setHeaders: { Authorization: `Bearer ${result.value!.token}` } });
          return next(retryReq);
        }),
        catchError(() => {
          tokenStorage.clearAll();
          router.navigate(['/login']);
          return EMPTY;
        })
      );
    })
  );
};
```

---

### `app.config.ts` – required providers

```typescript
providers: [
  provideBrowserGlobalErrorListeners(),
  provideRouter(routes, withHashLocation()),
  provideHttpClient(withInterceptors([authInterceptor])),
  providePrimeNG({ theme: { preset: Aura } }),
  MessageService,
]
```

**Hash-Based Routing:** Use `withHashLocation()` for SPAs deployed without server-side route handling. URLs use `#` (e.g. `/#/home`).
