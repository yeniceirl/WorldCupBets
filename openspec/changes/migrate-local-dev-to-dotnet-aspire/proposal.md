# Proposal: Migrate local development orchestration to .NET Aspire

## Problem Statement
The current local development flow depends on Docker/Podman Compose plus manual log inspection across backend, frontend, PostgreSQL, Redis, and Flyway. This has already slowed debugging for auth, migrations, proxying, and frontend/runtime issues. We need a more observable and developer-friendly local orchestration model.

## Intent
Adopt .NET Aspire for local development orchestration so the project gets a visual dashboard for logs, health, traces, and service wiring, while keeping the current product behavior intact.

## Scope
- Add an Aspire AppHost for local development only.
- Model the current stack: WebApi, PostgreSQL, Redis, and Angular frontend.
- Prefer Aspire-managed visibility for health, logs, and startup dependencies.
- Keep production/runtime deployment decisions out of scope for this first change.
- Decide explicitly what happens with Flyway in local dev: retire it, replace it, or keep it only for non-local workflows.

## Affected Areas
- Solution structure for Aspire host projects.
- Local development startup workflow.
- Environment/config wiring for API, frontend, PostgreSQL, Redis, and auth settings.
- Developer documentation for running the stack.

## Proposed Changes
1. Add a minimal Aspire AppHost and any required service defaults project.
2. Register the existing WebApi project in Aspire with its required configuration and health checks.
3. Register PostgreSQL and Redis as local development dependencies.
4. Run Angular through a Node dev command managed by Aspire for local development, replacing the local nginx-based frontend path.
5. Define a single local startup path that replaces the current “watch multiple containers and logs manually” experience.
6. Document how auth/dev-login, JWT secret, database password, and optional Google client configuration work under Aspire.
7. Keep EF Core migrations as the automatic schema path when the API starts in development, while leaving Flyway manual in local development for SQL artifacts such as views and stored procedures.
8. Remove the Compose/nginx local development path once Aspire is the accepted local workflow.

## Open Decisions
- Do we want Aspire only for development, or also as the long-term canonical orchestration entrypoint?

## Risks
- Introducing Aspire before stabilizing the development workflow could add another moving part.
- Frontend integration still requires explicit browser-to-API routing, even though Aspire manages process orchestration and service wiring.
- Rider or other GUI IDEs may not inherit the shell environment needed to locate Node/npm when Node is installed through tools such as nvm.

## Rollback
- Restore the previous Compose and Dockerfile-based local development entrypoint from version control if Aspire adoption stalls.

## Success Criteria
- A developer can start the local stack from Aspire and see all core services in one dashboard.
- Logs, health, and dependency status are easier to inspect than with the current Compose-only flow.
- The existing dev-login path and `/matches` flow still work end to end.
- The team has a clear documented decision on frontend hosting and Flyway’s role in local development.
