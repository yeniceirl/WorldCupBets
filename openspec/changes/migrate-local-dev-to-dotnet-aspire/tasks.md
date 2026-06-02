# Tasks: Migrate Local Development to .NET Aspire

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 600-750 including untracked AppHost/frontend/docs and deleted Compose/nginx files |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 AppHost/API wiring → PR 2 frontend proxy/dev server → PR 3 remove Compose/nginx + docs/verification |
| Delivery strategy | exception-ok |
| Chain strategy | size-exception |

Decision needed before apply: No
Chained PRs recommended: Yes
Chain strategy: size-exception
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Add Aspire AppHost and API dependency/config wiring | PR 1 | Include solution/package updates and API health/dependency verification. |
| 2 | Move frontend local serving to Aspire-managed Angular dev server | PR 2 | Depends on PR 1; include proxy/runtime env and browser smoke checks. |
| 3 | Retire Compose/nginx local path and document Aspire workflow | PR 3 | Depends on PR 2; include docs, cleanup, and acceptance verification. |

## Phase 1: Aspire Foundation

- [x] 1.1 Create `src/WorldCupBets.AppHost/WorldCupBets.AppHost.csproj` with Aspire AppHost, NodeJs, PostgreSQL, Redis, and user-secrets packages.
- [x] 1.2 Create `src/WorldCupBets.AppHost/Program.cs` to require `DB_PASSWORD`/`JWT_SECRET`, add PostgreSQL/Redis, wire WebApi config, health, endpoints, and waits.
- [x] 1.3 Modify `WorldCupBets.sln` and central package configuration to include the AppHost.

## Phase 2: Frontend Local Development

- [x] 2.1 Create `frontend/scripts/start-dev.js` to generate `.generated/env.js` and run `ng serve` under Aspire-provided env.
- [x] 2.2 Modify `frontend/proxy.conf.js` to proxy `/api` and `/health` to `ASPIRE_API_URL`, defaulting to `http://localhost:5000`.
- [x] 2.3 Modify `frontend/angular.json` and `frontend/package.json` so `npm start` uses the generated env asset and proxy.

## Phase 3: Local Workflow Cleanup

- [x] 3.1 Delete local `docker-compose.yml`, root `Dockerfile`, `frontend/Dockerfile`, `frontend/nginx.conf`, and `frontend/docker-entrypoint.sh` as offered local entrypoints.
- [x] 3.2 Remove obsolete `.env.example` and update `.gitignore` for `.env` and Aspire-generated frontend artifacts.
- [x] 3.3 Update `docs/local-development.md` with Aspire startup, secrets, ports, proxy behavior, EF/Flyway policy, and Rider/npm caveat.

## Phase 4: Verification / Review

- [x] 4.1 Verify missing `DB_PASSWORD` or `JWT_SECRET` fails AppHost startup with a clear error.
- [x] 4.2 Verify Rider-launched AppHost shows PostgreSQL, Redis, WebApi, and frontend in the Aspire dashboard.
- [x] 4.3 Browser-smoke frontend: dev login works and `/matches`, summary, and champion calls route through relative `/api` via Angular proxy.
- [x] 4.4 Review large diff as the size-exception work units above, keeping unrelated product/runtime fixes out of the Aspire PR when possible.
