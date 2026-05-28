# Implementation Tasks: implement-google-login-jwt

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 650-950 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 backend domain+persistence → PR 2 application login use case → PR 3 infrastructure+web API auth exchange → PR 4 frontend auth state/persistence → PR 5 frontend login/callback wiring |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

## Notes
- `openspec/config.yaml` reports `strict_tdd: false` and no reliable test runner; use build/manual verification tasks instead of strict RED → GREEN sequencing.
- Keep each slice near the requested ~300 changed-line review budget by deferring niceties (avatar handling, token decoding helpers, extra UI polish, extra docs) unless required by spec.
- Treat the EF migration as part of the backend persistence slice only; do not mix frontend changes into that review.

## Task Plan

1. **PR 1 — Add auth domain model and persistence schema**
   - Files: `src/WorldCupBets.Domain/Entities/User.cs`, `src/WorldCupBets.Domain/Entities/Role.cs`, `src/WorldCupBets.Domain/Entities/UserRole.cs`, `src/WorldCupBets.Infrastructure/Persistence/AppDbContext.cs`, `src/WorldCupBets.Infrastructure/Persistence/Configurations/UserConfiguration.cs`, `RoleConfiguration.cs`, `UserRoleConfiguration.cs`, discovery target `src/WorldCupBets.Infrastructure/Persistence/Migrations/*google*|*auth*`.
   - Work: add normalized user/role entities, expose `DbSet`s, enforce unique indexes/relationship mappings, seed `Admin` and `Bettor`, and generate the auth migration only for these tables/seed rows.
   - Verify: `dotnet build WorldCupBets.sln`; migration diff contains only `Users`, `Roles`, `UserRoles`, indexes, and seed data.
   - Rollback boundary: revert domain entity files, EF configurations, and the generated migration only.

2. **PR 2 — Add application login use case and auth contracts**
   - Files: `src/WorldCupBets.Application/Abstractions/IGoogleTokenValidator.cs`, `IJwtTokenGenerator.cs`, new auth files under `src/WorldCupBets.Application/Features/Auth/` such as `GoogleLoginCommand.cs`, `GoogleLoginHandler.cs`, `GoogleIdentity.cs`, `AuthResponseDto.cs`, and any validator file needed for blank `idToken` rejection.
   - Work: replace the scaffold `bool` validator contract with a `GoogleIdentity` result, upgrade JWT generation input to full auth context, and implement the command/handler that validates the token, provisions first-time users as `Bettor`, reuses returning users, loads roles, and returns the response DTO.
   - Verify: `dotnet build WorldCupBets.sln`; code review confirms no Google SDK or JWT library details leak into Application.
   - Rollback boundary: revert `Application/Features/Auth` and abstraction changes without touching Web API or Angular.

3. **PR 3 — Implement infrastructure auth adapters and the real Web API exchange**
   - Files: `src/WorldCupBets.Infrastructure/Authentication/GoogleTokenValidator.cs`, `JwtTokenGenerator.cs`, `src/WorldCupBets.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs`, `src/WorldCupBets.WebApi/Endpoints/AuthEndpoints.cs`, `src/WorldCupBets.WebApi/Extensions/ServiceCollectionExtensions.cs`, `src/WorldCupBets.WebApi/Configuration/JwtOptions.cs`, `GoogleOptions.cs`, discovery target `src/WorldCupBets.WebApi/appsettings*.json` if config templates need placeholders.
   - Work: implement Google ID token validation against `Google:ClientId`, generate signed JWTs with `sub`/`email`/`name`/role claims and expiry, tighten `AddJwtBearer` validation, replace the `501` auth placeholder with `POST /api/auth/google`, and map `400`/`401`/`200` responses per design.
   - Verify: `dotnet build WorldCupBets.sln`; manual API smoke confirms blank token returns `400`, invalid token returns `401`, and success shape matches design.
   - Rollback boundary: revert auth adapter and Web API files without reverting the migration PR.

4. **PR 4 — Add frontend auth persistence and authenticated API reuse**
   - Files: `frontend/src/app/core/auth/auth-state.service.ts`, `auth-token.interceptor.ts`, new files under `frontend/src/app/core/auth/` such as `auth.service.ts`, `auth.models.ts`, `auth-storage.ts`, plus `frontend/src/app/app.config.ts` if provider wiring changes.
   - Work: add the `/api/auth/google` exchange client, persist the backend JWT under one stable LocalStorage key, hydrate state on startup, clear bad/stale auth state on failed exchange, and keep the interceptor attaching `Authorization: Bearer` from runtime state.
   - Verify: `cd frontend && npm run build`; manual browser review confirms reload hydration logic is wired from LocalStorage, not hard-coded placeholder state.
   - Rollback boundary: revert `frontend/src/app/core/auth/**` and any provider wiring only.

5. **PR 5 — Replace auth page placeholders with the Google login/callback flow**
   - Files: `frontend/src/app/auth/login-page.component.ts`, `login-callback-page.component.ts`, `frontend/src/app/app.routes.ts`, and any small supporting auth UI file created under `frontend/src/app/auth/`.
   - Work: turn the placeholder pages into a real flow that obtains the Google ID token, posts it through the auth service, handles loading/error states, stores the returned JWT only after success, and routes the authenticated user away from the callback/login shell.
   - Verify: `cd frontend && npm run build`; manual end-to-end smoke confirms successful login persists the JWT and subsequent API requests include the bearer header.
   - Rollback boundary: revert auth page/callback wiring without touching backend code.

6. **Pre-apply review gate — enforce the review budget on every slice**
   - Discovery targets: all files above plus `openspec/changes/implement-google-login-jwt/design.md` and `openspec/changes/implement-google-login-jwt/specs/authentication/spec.md`.
   - Work: before each apply step, check estimated diff size against the ~300-line target, move optional helpers/polish into later slices if needed, and keep backend domain/application/infrastructure/webapi/frontend changes isolated to their planned PR.
   - Verify: each PR is independently reviewable and stays within a focused rollback boundary.
   - Rollback boundary: drop or split only the oversize PR without re-planning completed earlier slices.
