# Champion Settlement Specification

## Purpose

Define champion bet settlement and jackpot accounting.

## Requirements

### Requirement: Champion Jackpot Accounting

The system MUST track champion jackpot contributions from match settlements separately from champion bet stakes. Jackpot contributions MUST be available for final champion payout.

#### Scenario: Jackpot receives match contribution

- GIVEN a match settlement produces a jackpot contribution
- WHEN the settlement completes
- THEN the champion jackpot total increases by that contribution

### Requirement: Settle Champion Bets Once

At tournament completion, the system MUST settle champion bets exactly once. Correct champion bettors recover their 50 CC stake and split losing champion stakes plus the champion jackpot using integer division rounded down; any remainder MUST remain recorded as undistributed jackpot.

#### Scenario: Champion winners receive payout

- GIVEN the tournament champion is official and at least one champion bet is correct
- WHEN champion settlement runs
- THEN each correct bettor receives 50 CC plus an equal integer share of losing stakes and jackpot

#### Scenario: Prevent champion double settlement

- GIVEN champion settlement already completed
- WHEN champion settlement is requested again
- THEN balances and jackpot totals MUST remain unchanged
