# Code Review Rules

## General
- Keep commits grouped by behavior, not by file type.
- Keep tests with the code they verify.
- Prefer the smallest change that fully solves the problem.
- Do not commit temporary handoff files, local IDE metadata, or secrets.

## Backend
- Keep WebApi endpoints thin; business rules belong in Application or Domain.
- Prefer explicit repository contracts over leaking EF concerns upward.
- Add or update tests when changing rules, handlers, or auth behavior.
- Keep migrations deterministic and aligned with the current EF model snapshot.

## Frontend
- Use standalone Angular patterns already present in the repo.
- Keep API calls behind feature services; components should focus on UI state.
- Prefer clear loading, error, and empty states for user-facing pages.
- Add stable `data-testid` hooks only where they help E2E coverage.
