# RBAC System (.NET 8 + React/Vite)

Production-style RBAC demo with JWT auth, permission-based authorization, structured observability, activity feed, and CI.

## Stack

- Backend: ASP.NET Core (.NET 8), EF Core, PostgreSQL
- Frontend: React, Vite, TypeScript
- Auth: JWT (`localStorage.token`)
- Authorization: RBAC (roles + permissions + policy attribute)

## Local URLs

- Backend: `http://localhost:5083`
- Frontend: `http://localhost:5173`

## How To Run

### Backend

```bash
dotnet restore ProjetoC#.sln
dotnet build ProjetoC#.sln
dotnet run --project Rbac.Api/Rbac.Api.csproj
```

### Frontend

```bash
cd Rbac.Api/rbac-web
npm install
npm run dev
```

### Quality checks

```bash
dotnet test ProjetoC#.sln
cd Rbac.Api/rbac-web
npm run lint
npm run test
npm run build
```

## Error Contract (RFC 7807)

All backend errors return ProblemDetails with these fields:

- `traceId`
- `correlationId`
- `code` (example: `auth.invalid_credentials`, `rbac.forbidden`)
- `message` (friendly text)

Standardized for `401`, `403`, `404`, `500`.

## Correlation ID

- Header accepted/generated: `X-Correlation-Id`
- Frontend sends one per request
- Backend returns it in response header and error payload

## Default Dev Credentials

- Email: `admin@rbac.local`
- Password: `Admin@123`

In development, startup seed creates:

- roles: `Admin`, `User`
- default permissions
- admin user with full permissions

## Main Routes (Frontend)

- `/login` (public)
- `/` (protected)
- `/admin` (protected + admin/permission)
- `/playground`
- `/account`
- `/users`
- `/roles`
- `/permissions`
- `/access-denied`
- `/not-found`

## Main Endpoints (Backend)

### Auth

- `POST /auth/register`
- `POST /auth/login`

### User profile and users

- `GET /users/me`
- `GET /users`
- `GET /users/{id}`
- `POST /users/{id}/roles`
- `DELETE /users/{id}/roles/{roleId}`

### Roles and permissions

- `GET /roles`
- `POST /roles`
- `PUT /roles/{id}`
- `DELETE /roles/{id}`
- `PUT /roles/{id}/permissions`
- `GET /permissions`

### Activity feed

- `GET /activity?limit=50`
- `POST /activity`

## Default Permission Keys

- `admin.access`
- `users.me.read`
- `users.read`
- `users.roles.assign`
- `users.roles.remove`
- `roles.read`
- `roles.create`
- `roles.update`
- `roles.delete`
- `roles.permissions.update`
- `permissions.read`
- `activity.read`
- `activity.write`

## CI

GitHub Actions workflow (`.github/workflows/ci.yml`) runs:

1. Backend restore/build/test
2. Frontend `npm ci`
3. Frontend lint/test/build
