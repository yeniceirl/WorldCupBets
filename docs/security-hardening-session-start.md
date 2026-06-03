# Security Hardening Session Start

## Context

CopaCoin V1 is now functionally usable: users can place/change match bets before close, place champion bets, admins can import group fixtures, record results, and settle champion bets. The next session should focus on making the system harder to cheat.

Assume users are developers and the browser/client is hostile. Frontend checks are UX only; backend and database rules must enforce the real security boundary.

## Current Trust Model

- The CopaCoin database is the source of truth for matches, bets, balances, settlement, and admin actions.
- External football APIs are import/enrichment providers only.
- Admin-confirmed results remain the source of truth for settlement.
- Dev login exists for local development and must not become a production path.

## High-Priority Risks

1. Double settlement under concurrent admin requests.
2. Double betting or balance races under concurrent user requests.
3. Changing a match bet after the betting window closes via direct API calls.
4. Placing champion bets after the champion market closes via direct API calls.
5. Admin endpoints reachable by non-admin users because of missing/misconfigured policies.
6. Dev login exposed outside Development.
7. JWT/session hardening gaps: secret strength, token lifetime, role claims, Swagger auth behavior.
8. Import/admin actions mutating fixtures that already have bets without explicit safety rules.

## Backend Hardening Checklist

- Add explicit transaction boundaries around balance mutations, bet placement/change, result recording, and settlement.
- Add optimistic concurrency tokens or row-level locking for `User`, `Match`, `MatchBet`, and `TournamentSettlement` where needed.
- Make settlement idempotent and safe under concurrent calls.
- Ensure `PlaceMatchBetHandler` re-checks match betting window and existing bet state inside the same transaction used for mutation.
- Ensure changing an existing bet cannot change stake, user, match, or `PlacedAtUtc`.
- Audit every endpoint for `RequireAuthorization` and admin-only policy usage.
- Add tests for direct API/business-rule attempts, not just UI flows.
- Decide fixture import safety when imported fixture already has bets: block, require preview/confirm, or allow only non-critical metadata updates.

## Frontend Hardening Checklist

- Treat frontend disabled buttons as UX only.
- Show clear locked states after betting windows close.
- Add user-facing feedback when a backend rejects an attempted change after close.
- Avoid exposing dev-only affordances unless `ENABLE_DEV_LOGIN` and backend environment agree.

## Suggested First Work Unit

Start with settlement concurrency because it can create money or pay winners twice.

Target behavior:

- Two concurrent `record match result` requests for the same match must not settle twice.
- Two concurrent champion settlement requests must not pay twice.
- Tests should prove second caller receives idempotent/no-op behavior or a clear conflict.

## Relevant Files

- `src/WorldCupBets.Application/Features/Matches/RecordMatchResultHandler.cs`
- `src/WorldCupBets.Application/Features/Bets/SettleChampionHandler.cs`
- `src/WorldCupBets.Application/Features/Bets/PlaceMatchBetHandler.cs`
- `src/WorldCupBets.Domain/Entities/Match.cs`
- `src/WorldCupBets.Domain/Entities/MatchBet.cs`
- `src/WorldCupBets.Domain/Entities/User.cs`
- `src/WorldCupBets.Domain/Entities/TournamentSettlement.cs`
- `src/WorldCupBets.Infrastructure/Persistence/AppDbContext.cs`
- `src/WorldCupBets.WebApi/Endpoints/*Endpoints.cs`
- `tests/WorldCupBets.Application.Tests/*`
