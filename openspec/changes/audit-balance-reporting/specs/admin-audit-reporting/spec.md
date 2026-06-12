# Admin Audit Reporting Specification

## Purpose

Provide an admin-only report that explains each user's current balance from existing betting state. This report SHALL be explicitly derived from current state and MUST NOT claim immutable accounting history.

## Requirements

### Requirement: Admin derived balance summary

The system MUST provide an admin-only major report with one row per user showing available balance, pending total, derived total balance, total won, and total lost. Reported values MUST be derived from the user's current balance and currently stored betting entities, and MUST be labeled as derived current-state values rather than immutable ledger history.

#### Scenario: Admin opens the major report
- GIVEN an authenticated admin and users with settled and unsettled betting activity
- WHEN the admin requests the audit balance summary
- THEN the system returns one row per user with available, pending, derived total, won, and lost values
- AND the response identifies the report as derived from current state

#### Scenario: Summary reflects current state after settlement changes
- GIVEN a user's settled or pending betting state has changed
- WHEN an admin requests the audit balance summary afterward
- THEN the returned values reflect the current stored state
- AND the report does not expose or imply immutable historical reconstruction

### Requirement: Admin user subledger drill-down

The system MUST let an admin open a per-user subledger that lists derived detail lines across match bets, challenges, champion picks, and special player picks. Each detail line MUST show enough information to explain whether the item is won, lost, refunded, or pending and how it contributes to the derived report.

#### Scenario: Admin drills into one user
- GIVEN an authenticated admin and a user with activity in multiple betting sources
- WHEN the admin requests that user's audit subledger
- THEN the system returns detail lines for the user's supported betting sources
- AND each line shows the source, stake, derived outcome, and pending amount or settled contribution

#### Scenario: User with no activity still has an explainable view
- GIVEN an authenticated admin and a user with no relevant betting activity
- WHEN the admin requests that user's audit subledger
- THEN the system returns the user's summary context with no detail lines
- AND the response remains a successful empty drill-down

### Requirement: Pending reasons are explicit

The system MUST provide a human-readable pending reason for every pending subledger item. Pending reasons MUST describe the unresolved dependency from current domain state.

#### Scenario: Pending reasons distinguish unresolved states
- GIVEN an authenticated admin views a user subledger containing unsettled match bets, open or matched challenges, unsettled champion picks, and special player picks
- WHEN the drill-down is returned
- THEN each pending line includes a human-readable reason tied to its unresolved state
- AND different unresolved states are not collapsed into a single generic pending label

#### Scenario: Settled items do not show a pending reason
- GIVEN an authenticated admin views a user subledger containing settled or refunded items
- WHEN the drill-down is returned
- THEN those terminal items do not require a pending reason
- AND their derived outcome remains visible from current stored state

### Requirement: Authorization is admin-only

The system MUST restrict both the major report and the per-user subledger to callers authorized by the Admin policy. Unauthorized callers MUST NOT receive audit summary data, subledger data, or pending-reason details.

#### Scenario: Non-admin is rejected from summary
- GIVEN a caller is unauthenticated or lacks Admin authorization
- WHEN the caller requests the audit balance summary
- THEN the system rejects the request with an authorization error
- AND no audit data is returned

#### Scenario: Non-admin is rejected from drill-down
- GIVEN a caller is unauthenticated or lacks Admin authorization
- WHEN the caller requests a user's audit subledger
- THEN the system rejects the request with an authorization error
- AND no per-user audit detail is returned

## Acceptance Criteria

- Admins can view a per-user summary report of derived balance values.
- Admins can drill into one user and inspect derived subledger lines across supported betting sources.
- Pending items always include a human-readable reason tied to the unresolved current-state dependency.
- The feature remains explicitly derived reporting, not immutable accounting history.
- Non-admin callers cannot access either report surface.
