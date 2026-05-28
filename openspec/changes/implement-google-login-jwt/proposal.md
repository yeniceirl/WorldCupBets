# Proposal: Implement Google login JWT

## Problem Statement
The current scaffold exposes Google auth placeholders only. We need a concrete, scaffold-aligned plan for the first real authentication slice so users can sign in with Google and receive a backend-issued JWT.

## Intent
Implement the first production auth flow for Google login only: validate the Google ID token, auto-provision new users as `Bettor`, issue a JWT access token, and let the Angular app persist that token in LocalStorage for this first cut.

## Scope
- Implement `POST /api/auth/google` end to end.
- Support login only; do not add refresh tokens, logout orchestration, or broader account management.
- Auto-provision first-time Google users with the `Bettor` role.
- Keep Admin bootstrap out of scope; the first Admin will be assigned manually in the database.
- Store the frontend access token in LocalStorage for this initial version.

## Affected Areas
- `src/WorldCupBets.Application` auth contracts/handlers/DTOs
- `src/WorldCupBets.Infrastructure` Google validation, JWT issuance, persistence, EF mappings
- `src/WorldCupBets.WebApi` auth endpoint and auth configuration
- `frontend/src/app/core/auth` auth state, token persistence, interceptor updates
- `frontend/src/app/auth` Google login and callback flow
- PostgreSQL schema/migrations for user identity and role storage

## Proposed Changes
1. Replace the scaffold-only Google auth endpoint with a real login flow in the Minimal API/Clean Architecture stack.
2. Add application-level auth request/response models and a Wolverine-driven login use case.
3. Implement infrastructure services to validate Google ID tokens and generate signed JWT access tokens.
4. Add persistence support for app users and roles so Google users can be found or auto-created as `Bettor`.
5. Embed role claims in the JWT so existing `Admin` and `Bettor` policies can protect future endpoints.
6. Update the Angular auth flow to complete Google sign-in, exchange the ID token with the backend, and persist the returned JWT in LocalStorage.
7. Keep the UI and API contracts explicit that this is access-token-only authentication with no refresh token yet.

## Risks
- Google token validation and JWT configuration errors could block all sign-in attempts.
- LocalStorage token storage is acceptable for this first cut but has stronger XSS exposure than more mature session approaches.
- Manual first-Admin assignment creates an operational dependency on correct database setup.

## Rollback
- Revert the Google login endpoint, auth services, and frontend token persistence changes.
- Remove any user/role schema additions or roll back the related EF migration.
- Return the frontend auth surface to scaffold-only behavior if needed.

## Success Criteria
- A user can sign in with Google and receive a valid JWT from `POST /api/auth/google`.
- A new Google user is automatically provisioned once with the `Bettor` role.
- Existing users can log in repeatedly without duplicate provisioning.
- The Angular app persists the JWT in LocalStorage and sends it on authenticated API calls.
- No refresh token flow or automatic Admin bootstrap is introduced in this change.
