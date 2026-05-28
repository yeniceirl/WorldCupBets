# Authentication Specification

## Purpose

Define the first production authentication slice for Google login, backend-issued JWT access tokens, automatic `Bettor` provisioning for new users, and frontend token persistence for authenticated API calls.

## Requirements

### Requirement: Google login exchange

The system MUST expose a Google-login exchange that accepts a Google ID token and returns an application JWT access token for successful sign-in.

#### Scenario: Successful Google login

- GIVEN a client has obtained a valid Google ID token for a Google account
- WHEN the client submits the token to `POST /api/auth/google`
- THEN the system returns a successful response containing an application JWT access token
- AND the response identifies the authenticated application user

#### Scenario: Invalid Google token

- GIVEN a client submits a missing, malformed, expired, or untrusted Google ID token
- WHEN the token is processed by `POST /api/auth/google`
- THEN the system rejects the request
- AND the system does not create or update an application user
- AND the system does not issue an application JWT

### Requirement: User auto-provisioning

The system MUST automatically provision a first-time Google user as an application user with the `Bettor` role.

#### Scenario: First-time Google user

- GIVEN a valid Google ID token for a Google account that is not yet linked to an application user
- WHEN the client completes `POST /api/auth/google`
- THEN the system creates exactly one new application user for that Google account
- AND the new user is assigned the `Bettor` role
- AND the system issues an application JWT for that user

#### Scenario: Returning Google user

- GIVEN a valid Google ID token for a Google account that is already linked to an application user
- WHEN the client completes `POST /api/auth/google`
- THEN the system reuses the existing application user
- AND the system does not create a duplicate user
- AND the system issues an application JWT for that user

### Requirement: JWT role claims

The system MUST issue application JWT access tokens that include the authenticated user's authorization roles.

#### Scenario: Bettor role token

- GIVEN an authenticated application user with the `Bettor` role
- WHEN the system issues an application JWT
- THEN the JWT contains a role claim for `Bettor`

#### Scenario: Admin role token

- GIVEN an authenticated application user with the `Admin` role
- WHEN the system issues an application JWT
- THEN the JWT contains a role claim for `Admin`

### Requirement: Frontend token persistence and reuse

The frontend MUST exchange the Google ID token for the backend JWT, persist the JWT in LocalStorage, and send the stored JWT on authenticated API requests.

#### Scenario: Token stored after login

- GIVEN a user completes Google sign-in in the frontend
- WHEN the frontend receives a successful response from `POST /api/auth/google`
- THEN the frontend stores the returned JWT in LocalStorage
- AND the frontend treats the user as authenticated for subsequent app usage

#### Scenario: Authenticated API call

- GIVEN the frontend has a JWT stored in LocalStorage
- WHEN the frontend sends a request to an authenticated backend API
- THEN the frontend includes the stored JWT as the bearer token

### Requirement: Explicit non-goals for this change

The system MUST keep this change limited to access-token login and MUST NOT introduce refresh tokens, logout or session-hardening workflows, or automatic admin bootstrap.

#### Scenario: No refresh token issued

- GIVEN a user completes Google login successfully
- WHEN the backend returns the authentication response
- THEN the response contains an access token only
- AND the response does not include a refresh token

#### Scenario: Admin bootstrap remains manual

- GIVEN the system has no application user with the `Admin` role
- WHEN a new Google user signs in for the first time
- THEN the user is provisioned as `Bettor`
- AND the system does not automatically assign `Admin`
- AND first `Admin` assignment remains a manual database operation
