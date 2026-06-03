# CopaCoin Leaderboard Specification

## Purpose

Define the V1 leaderboard based on current CopaCoin balances.

## Requirements

### Requirement: Order By Current CopaCoin

The system MUST show the leaderboard ordered by current CopaCoin balance descending. The displayed balance MUST include settled match payouts, champion payouts, refunds, jackpot effects, and rescue adjustments already applied to the user balance.

#### Scenario: Highest current balance first

- GIVEN users have different current CopaCoin balances
- WHEN the leaderboard is requested
- THEN users appear from highest balance to lowest balance

#### Scenario: Reflect settlement changes

- GIVEN a match or champion settlement changes user balances
- WHEN the leaderboard is requested after settlement
- THEN the ordering uses the updated balances

#### Scenario: Equal balances

- GIVEN two users have the same current CopaCoin balance
- WHEN the leaderboard is requested
- THEN both users appear with that balance
- AND no advanced tie-breaker is required for V1
