# Match Challenges Specification

## Purpose

Enable authenticated users to create generic, free-text, match-scoped retos with friends using CopaCoins, without requiring predefined bet types or extending winner bets/tournament picks.

## Requirements

### Requirement: Custom Challenge Creation

The system MUST allow an authenticated user to create an `Open` match challenge with a match, free-text claim, equal stake amount, creator side text, and opposite taker side text while the match betting window is open. The system MUST escrow the creator stake immediately and MUST reject invalid match, closed betting window, empty/overlong text, non-positive stake, or insufficient CopaCoins. Challenge text MUST remain generic/user-authored, not selected from predefined bet categories.

#### Scenario: Creator opens a valid challenge

- GIVEN an authenticated user with enough available CopaCoins and a valid match
- WHEN they submit claim text, creator side, taker side, and stake
- THEN the challenge is created as `Open`
- AND the creator stake is escrowed and no taker is assigned

#### Scenario: Creation is rejected

- GIVEN an authenticated user with an invalid payload or insufficient available CopaCoins
- WHEN they submit a challenge
- THEN no challenge is created
- AND no CopaCoins are escrowed

#### Scenario: Creation is rejected after the match window closes

- GIVEN a match whose betting window is closed
- WHEN an authenticated user submits a challenge for that match
- THEN no challenge is created
- AND no CopaCoins are escrowed

### Requirement: Challenge Listing and Acceptance

The system MUST let authenticated users list match challenges and accept an `Open` challenge as the opposite side while the match betting window is open. Acceptance MUST require a non-creator user, sufficient available CopaCoins, and exactly the same stake as the creator. The system MUST escrow the taker stake immediately, transition the challenge to `Matched`, and prevent double acceptance.

#### Scenario: Taker accepts an open challenge

- GIVEN an `Open` challenge and a different authenticated user with enough CopaCoins
- WHEN the user accepts the challenge
- THEN the challenge becomes `Matched`
- AND both creator and taker stakes are escrowed

#### Scenario: Acceptance race or self-acceptance is rejected

- GIVEN a challenge that is already matched, not open, or owned by the accepting user
- WHEN acceptance is attempted
- THEN acceptance fails
- AND no additional CopaCoins are escrowed

#### Scenario: Acceptance is rejected after the match window closes

- GIVEN an `Open` challenge for a match whose betting window is closed
- WHEN another user attempts acceptance
- THEN acceptance fails
- AND no CopaCoins are escrowed

### Requirement: Creator Challenge Cancellation

The system MUST allow the creator to cancel only their own `Open` challenge and refund the creator escrow. The system MUST reject cancellation by non-creators and MUST reject cancellation after the challenge is matched or terminal. Challenge editing MUST NOT be supported; users cancel and recreate instead.

#### Scenario: Creator cancels an open challenge

- GIVEN an `Open` challenge with creator escrow
- WHEN the creator cancels it
- THEN the challenge becomes `Voided`
- AND the creator escrow is refunded

#### Scenario: Cancellation is rejected

- GIVEN a challenge owned by another user or no longer `Open`
- WHEN cancellation is attempted
- THEN cancellation fails
- AND no CopaCoins are refunded

### Requirement: Manual Lifecycle Settlement

The system MUST support statuses `Open`, `Matched`, `Settled`, `Voided`, and `Expired`. Only admins SHALL settle, void, or expire challenges in V1. Settlement MUST choose either creator side or taker side as winner and pay the full escrow to that side. Voiding or expiring MUST refund all unsettled escrow to current participants. Terminal challenges MUST NOT be accepted, settled again, voided again, or expired again. The system MUST NOT auto-settle from match results.

#### Scenario: Admin settles a matched challenge

- GIVEN a `Matched` challenge with two escrowed stakes
- WHEN an admin settles it for the creator or taker side
- THEN the selected side receives both stakes
- AND the challenge becomes `Settled`

#### Scenario: Admin voids or expires a challenge

- GIVEN an `Open` or `Matched` challenge with escrowed stakes
- WHEN an admin voids or expires it
- THEN all escrowed stakes are refunded to participants
- AND the challenge becomes `Voided` or `Expired`

### Requirement: Pending Stake Reporting

The system MUST include active challenge escrow in each participant's pending-stake reporting until the challenge reaches a terminal status. User-facing challenge actions SHOULD expose enough result state for clients to refresh wallet balances after creation, acceptance, settlement, voiding, or expiry.

#### Scenario: Pending stake includes active challenge escrow

- GIVEN a user has an `Open` or `Matched` challenge stake in escrow
- WHEN pending stake totals are requested
- THEN the challenge stake is included
- AND it is removed after `Settled`, `Voided`, or `Expired`

## Acceptance Criteria

- Users can create and accept binary, free-text match retos with equal escrowed stakes only while the match betting window is open.
- Creators can cancel open retos and recover escrow before another user accepts.
- Admins can settle, void, or expire challenges with correct payout/refund behavior.
- Active challenge stakes appear in pending totals and terminal stakes do not.
