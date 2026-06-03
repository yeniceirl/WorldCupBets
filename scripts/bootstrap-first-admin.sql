-- Bootstrap the first production admin for a new environment.
--
-- Replace FIRST_ADMIN_EMAIL@example.com with the Google account email that should
-- receive Admin access, then run this once after applying EF migrations.
-- The user row is created on first Google login because GoogleSubject is only
-- known after Google validates the account.

INSERT INTO user_invitations ("Email", "RoleName")
VALUES (UPPER(TRIM('FIRST_ADMIN_EMAIL@example.com')), 'Admin')
ON CONFLICT ("Email") DO UPDATE
SET "RoleName" = 'Admin';
