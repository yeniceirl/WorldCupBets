# Technical Design: implement-google-login-jwt

## Summary
Implement the first real auth slice: the frontend gets a Google ID token, exchanges it at `POST /api/auth/google`, the backend validates the Google token, finds or creates the app user as `Bettor`, issues a signed JWT with role claims, and the frontend stores that JWT in LocalStorage for later API calls.

## Backend design

### Endpoint contract
`POST /api/auth/google`

Request:
```json
{ "idToken": "<google-id-token>" }
```

Success `200 OK`:
```json
{
  "accessToken": "<jwt>",
  "user": {
    "id": 123,
    "email": "user@example.com",
    "displayName": "Ada Lovelace",
    "roles": ["Bettor"]
  }
}
```

Failure:
- `400` for missing/blank `idToken`
- `401` for invalid, expired, wrong-audience, or untrusted Google token
- no refresh token field

### Use-case flow
1. WebApi endpoint binds `GoogleLoginRequest`.
2. Application handler `GoogleLoginCommand` calls a Google validation seam.
3. Validation returns trusted Google identity data (`sub`, `email`, `name`, optional avatar).
4. Handler loads user by Google subject.
5. If missing, create user and attach `Bettor` role.
6. Load effective roles.
7. Generate app JWT.
8. Return `AuthResponseDto`.

## Google ID token validation seam
Replace the scaffold `bool` validator with a richer seam:
- `IGoogleTokenValidator.ValidateAsync(idToken)`
- returns `GoogleIdentity` payload, not just true/false
- implementation wraps `Google.Apis.Auth` and validates configured `Google:ClientId`

`GoogleIdentity` should expose at least:
- `Subject`
- `Email`
- `DisplayName`
- `EmailVerified`

This keeps Google SDK specifics in Infrastructure and gives the app layer a stable contract.

## User persistence model
Use normalized auth tables in EF Core:
- `Users` (`Id`, `GoogleSubject`, `Email`, `DisplayName`)
- `Roles` (`Id`, `Name`) seeded with `Admin` and `Bettor`
- `UserRoles` (`UserId`, `RoleId`)

Key constraints:
- unique index on `Users.GoogleSubject`
- unique index on `Roles.Name`
- unique composite on `UserRoles (UserId, RoleId)`

Why normalized roles instead of a single string column:
- matches existing policy-based auth
- supports future multi-role users
- allows manual DB promotion to `Admin` without schema changes

## JWT generation and claims
Upgrade `IJwtTokenGenerator` to accept the full auth context, not only user id.

JWT contents:
- `sub` = app user id
- `email`
- `name`
- role claims for each assigned role
- standard `iat`/`exp`

Signing:
- symmetric signing with `Jwt:Secret`
- enable real issuer/audience/lifetime/signature validation in `AddJwtBearer`
- add config for issuer, audience, and token lifetime if missing

Role handling:
- first-time Google users always get `Bettor`
- existing DB roles are preserved
- no automatic `Admin` bootstrap
- manual DB assignment to `Admin` appears in the next JWT issued at login

## Frontend auth flow
1. Login page triggers Google sign-in and obtains an ID token.
2. Callback page or auth service posts `{ idToken }` to `/api/auth/google`.
3. On success, store `accessToken` in LocalStorage and update in-memory auth state with token + user.
4. On app startup, hydrate auth state from LocalStorage.
5. HTTP interceptor attaches `Authorization: Bearer <token>`.
6. If login exchange fails, clear any stored token and show an auth error.

### LocalStorage handling
Use a single stable key, e.g. `worldcupbets.auth.accessToken`.
- write only after successful exchange
- clear on invalid token response or explicit local sign-out later
- treat LocalStorage as the persistence source and signal state as the runtime source

For this slice, storing only the JWT is enough; user info can be decoded from the token or cached in memory from the login response.

## Expected file changes
- `src/WorldCupBets.Application`
  - auth command/handler, DTOs, richer auth abstractions
- `src/WorldCupBets.Domain`
  - `User`, `Role`, `UserRole` entities
- `src/WorldCupBets.Infrastructure`
  - Google validator, JWT generator, EF configs, migration, optional role seeding
- `src/WorldCupBets.WebApi`
  - real auth endpoint, JWT bearer configuration, auth response mapping
- `frontend/src/app/core/auth`
  - auth service/state, LocalStorage hydration, interceptor reuse
- `frontend/src/app/auth`
  - real login and callback flow

## Tests
- application test: first login provisions `Bettor`
- application test: returning Google subject does not create duplicate user
- infrastructure test: JWT contains expected role claims
- web test: invalid Google token returns `401`
- frontend test: successful login stores token and interceptor sends bearer header

## Rollout and migration
- add EF migration for auth tables and role seed data
- deploy config for Google client id and JWT secret before enabling login
- manual post-deploy step: promote first admin in DB

## Tradeoffs and risks
- LocalStorage is simple and fits this slice, but has XSS exposure
- normalized roles add a bit more schema now, but avoid a later auth rewrite
- no refresh token means users must re-login after expiry
- Google/JWT config mistakes can break all sign-in until corrected
