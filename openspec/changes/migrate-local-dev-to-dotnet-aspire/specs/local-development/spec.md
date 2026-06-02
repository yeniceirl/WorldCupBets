# Local Development Specification

## Purpose

Define the accepted local development workflow after migrating orchestration to .NET Aspire.

## Requirements

### Requirement: Aspire canonical local entrypoint

The system MUST use the Aspire AppHost as the canonical local development entrypoint for the full stack.

#### Scenario: Full stack starts from Aspire

- GIVEN required local secrets are configured
- WHEN a developer starts the Aspire AppHost
- THEN PostgreSQL, Redis, the Web API, and the Angular frontend SHALL be started or represented in Aspire
- AND service status, logs, and health SHALL be inspectable from the Aspire dashboard

#### Scenario: Required secrets are missing

- GIVEN `DB_PASSWORD` or `JWT_SECRET` is not configured for local development
- WHEN the Aspire AppHost starts
- THEN startup SHALL fail with a clear missing-configuration error

### Requirement: Local service wiring acceptance criteria

The local stack MUST wire application dependencies through Aspire-managed configuration and startup ordering.

#### Scenario: API dependencies are available

- GIVEN Aspire starts the Web API
- WHEN the API is launched
- THEN it SHALL receive PostgreSQL and Redis connection configuration from Aspire
- AND it SHALL wait for those dependencies before being considered ready

#### Scenario: Authentication configuration is local-safe

- GIVEN the Aspire AppHost is configured with local user-secrets
- WHEN the API starts locally
- THEN `JWT_SECRET` SHALL configure JWT signing
- AND optional Google client and dev-login settings MAY be supplied by local configuration

### Requirement: Angular development proxy acceptance criteria

The frontend MUST run as an Aspire-managed npm development process and MUST route browser API calls through the Angular dev-server proxy.

#### Scenario: Frontend served by Angular dev server

- GIVEN Aspire starts the frontend resource
- WHEN a developer opens the frontend URL
- THEN the Angular development server SHALL serve the application
- AND the frontend SHALL remain visible as an Aspire-managed resource

#### Scenario: Browser API calls are proxied

- GIVEN the frontend calls relative `/api` URLs
- WHEN the browser sends an API request during local development
- THEN the Angular dev-server proxy SHALL forward `/api` traffic to the Aspire-managed API endpoint
- AND frontend code SHALL NOT require hard-coded backend origins

### Requirement: Local database migration policy

The API MUST apply EF Core migrations automatically on startup in local development, while Flyway SHALL remain manual and local-only for SQL artifacts.

#### Scenario: EF migrations run on API startup

- GIVEN the local database exists
- WHEN the API starts in local development
- THEN pending EF Core migrations SHALL be applied before normal API usage

#### Scenario: Manual SQL artifact changes

- GIVEN a local SQL artifact such as a view or stored procedure must be applied
- WHEN a developer needs that artifact locally
- THEN Flyway MAY be run manually
- AND Flyway SHALL NOT be the automatic local schema migration path

### Requirement: Removed local container path

The local development workflow MUST NOT depend on Docker Compose, local nginx, or local application Dockerfiles.

#### Scenario: Compose path is not offered

- GIVEN a developer follows local development documentation
- WHEN they start the project locally
- THEN the documented path SHALL use Aspire
- AND Docker Compose or nginx SHALL NOT be required for local development

### Requirement: Production deployment exclusion

This change MUST NOT define production deployment behavior.

#### Scenario: Production remains separate

- GIVEN the Aspire local workflow is accepted
- WHEN production deployment is considered
- THEN deployment requirements SHALL be handled by a separate change
