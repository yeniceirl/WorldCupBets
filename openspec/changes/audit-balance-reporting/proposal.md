# Proposal: Audit Balance Reporting

## Intent

Problem: Admins can see current balances and some pending data, but cannot inspect a derived per-user audit view explaining available balance, won, lost, and pending amounts from existing bet data. This change adds a safe read-only admin report, not a new accounting ledger.

## Scope

### In Scope
- Admin-only major view with one row per user: available balance, pending total, derived total balance, total won, and total lost.
- Admin-only subledger drill-down for each user across match bets, challenges, champion picks, and special player picks.
- Human-readable pending reasons showing what unresolved result or settlement each pending item depends on.
- Clear UI/API labeling that values are derived from current domain state.

### Out of Scope
- New immutable transaction ledger, backfill, or accounting-grade reconciliation history.
- Any balance mutation, settlement-rule change, or non-admin exposure.
- CSV export, filters beyond the first useful slice, or financial reporting outside betting entities already stored.

## Capabilities

### New Capabilities
- `admin-audit-reporting`: Admin-only derived balance reporting with user-level summary totals and per-user subledger detail sourced from existing betting data.

### Modified Capabilities
- None.

## Approach

Add thin `/api/admin/audit/*` read endpoints backed by application query handlers and repository projections. Reuse existing balance/pending rules from leaderboard and bet-settlement behavior to derive summary and detail rows. Keep terminology explicit: this report explains current-state balances from stored bets and settlements; it does not claim immutable accounting history.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/WorldCupBets.WebApi/Endpoints/AdminEndpoints.cs` | Modified | Admin audit endpoints. |
| `src/WorldCupBets.Application/Features/Admin` | New/Modified | Read-model queries, DTOs, handlers. |
| `src/WorldCupBets.Domain/Repositories/*` | Modified | Summary/detail projections for users and bet sources. |
| `frontend/src/app/features/admin/*` | Modified | Audit table, drill-down, and models. |
| `tests/WorldCupBets.Application.Tests/*` | Modified | Auth metadata and derived-balance coverage. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Derived won/lost values are imperfect where no transaction record exists | Med | Label as derived reporting and limit scope to explainable values. |
| Sensitive user bet data leaks outside admin | Low | Keep endpoints under Admin policy and extend authorization tests. |
| Large reports become query-heavy | Med | Use projection queries, not entity-by-entity loading. |

## Rollback Plan

Remove the admin audit endpoints and UI entry points, then revert the read-model/query changes. No data migration or balance rollback is required because the feature is read-only.

## Dependencies

- Existing admin authorization, current balance fields, and stored match/challenge/tournament-pick settlement data.

## Success Criteria

- [ ] Admins can open a per-user balance report showing available, pending, won, lost, and derived total values.
- [ ] Admins can drill into a user's subledger and see why each pending item is still unresolved.
- [ ] The feature ships without changing any balance-writing or settlement behavior.
