# Architecture Scaffold Specification

## Purpose

Define the accepted day-one scaffold for the WorldCupBets repository so backend, frontend, and local runtime boundaries are present and runnable before feature implementation begins.

## Requirements

### Requirement: Repository Scaffold Boundaries

The repository MUST expose the agreed root scaffold for solution, backend, frontend, and database work without introducing feature-complete business behavior.

#### Scenario: Root structure is present

- GIVEN the change is applied
- WHEN a contributor inspects the repository root
- THEN `src/`, `frontend/`, `database/`, and `WorldCupBets.sln` SHALL exist at the repository root
- AND shared build or repository convention files required by the scaffold SHALL exist at the repository root

#### Scenario: Backend project boundaries are established

- GIVEN the change is applied
- WHEN a contributor inspects the backend solution
- THEN the solution SHALL contain `WorldCupBets.Domain`, `WorldCupBets.Application`, `WorldCupBets.Infrastructure`, and `WorldCupBets.WebApi`
- AND project references SHALL reflect the intended Clean Architecture separation
- AND service contracts SHALL live in `WorldCupBets.Application`

### Requirement: Backend Scaffold Acceptance Criteria

The backend scaffold MUST provide composition and extension points for agreed cross-cutting concerns while remaining placeholder-only for product features.

#### Scenario: Day-one backend composition is available

- GIVEN the scaffolded backend projects
- WHEN the Web API startup and composition root are reviewed
- THEN extension points or registrations for Wolverine, FluentValidation, Mapster, HybridCache, Redis, JWT authentication, Swagger, global exception handling, and `/health` SHALL be present
- AND persistence, cache, auth, and application middleware wiring SHALL be scaffolded in their expected backend layers

#### Scenario: EF Core migration support is prepared without app data shape delivery

- GIVEN the scaffolded backend and database assets
- WHEN a contributor inspects persistence and migration setup
- THEN EF Core SHALL be configured for future migrations in the expected location
- AND no initial EF Core migration file SHALL be present as part of this change

### Requirement: Frontend Scaffold Acceptance Criteria

The frontend scaffold MUST provide a standalone application skeleton with authenticated entry points and placeholder-only feature areas.

#### Scenario: Frontend structure supports auth and future features

- GIVEN the scaffolded frontend
- WHEN a contributor inspects the frontend application structure
- THEN an Angular standalone application shell SHALL exist
- AND an auth shell and JWT interceptor skeleton SHALL be present
- AND lazy route or folder placeholders SHALL exist for matches, bets, leaderboard, and admin

#### Scenario: Non-auth product features remain unimplemented

- GIVEN the scaffolded frontend
- WHEN a contributor inspects the non-auth feature areas
- THEN those areas SHALL be placeholders only
- AND no feature-complete user workflows for matches, bets, leaderboard, or admin SHALL be delivered by this change

### Requirement: Container and Runtime Wiring Acceptance Criteria

The repository MUST define a runnable local day-one stack for backend, frontend, and supporting services.

#### Scenario: Runtime stack definition is complete

- GIVEN the change is applied
- WHEN a contributor inspects the container and runtime assets
- THEN definitions SHALL exist for backend, frontend, PostgreSQL, Redis, and Flyway
- AND the stack SHALL be intended to run together for local bootstrap validation

#### Scenario: nginx proxy scope is constrained

- GIVEN the frontend runtime proxy configuration
- WHEN HTTP routing behavior is reviewed
- THEN nginx SHALL proxy only `/api` and `/health` to the backend
- AND Swagger SHALL remain directly exposed from the backend rather than proxied through nginx
- AND SPA fallback behavior SHALL remain available for frontend routes

### Requirement: Explicit Non-Goals

This change MUST remain a scaffold-only bootstrap and MUST NOT be accepted if it expands into feature delivery.

#### Scenario: Out-of-scope work is excluded

- GIVEN the approved scaffold scope
- WHEN acceptance is evaluated
- THEN no initial EF Core migration SHALL be created
- AND no business feature implementation for betting, match management, leaderboard behavior, or administration SHALL be required
- AND no nginx proxy behavior beyond `/api` and `/health` SHALL be required
- AND no requirement to serve Swagger through nginx SHALL be introduced
