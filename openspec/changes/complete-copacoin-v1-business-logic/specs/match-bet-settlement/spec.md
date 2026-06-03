# Match Bet Settlement Specification

## Purpose

Define CopaCoin payouts for V1 match winner bets.

## Requirements

### Requirement: Settle Match Bets Once

For a resulted match, the system MUST settle eligible match bets exactly once using integer CopaCoin. Winners recover stake and split the losing pool. Integer division MUST round down; any indivisible remainder MUST be added to the champion jackpot. A second settlement attempt MUST NOT change balances.

#### Scenario: Winners split losing pool

- GIVEN a settled match with winners and losers
- WHEN settlement runs
- THEN each winner receives their stake plus an equal integer share of losing stakes
- AND any split remainder goes to the champion jackpot

#### Scenario: All bettors are correct

- GIVEN every participant bet on the official outcome
- WHEN settlement runs
- THEN each participant receives only their stake back
- AND no profit or jackpot contribution is created

#### Scenario: Nobody is correct

- GIVEN no participant bet on the official outcome
- WHEN settlement runs
- THEN each participant receives half their stake rounded down
- AND the remaining stake from each bet goes to the champion jackpot

#### Scenario: Prevent double settlement

- GIVEN a match settlement already completed
- WHEN settlement is requested again
- THEN user balances and champion jackpot MUST remain unchanged
