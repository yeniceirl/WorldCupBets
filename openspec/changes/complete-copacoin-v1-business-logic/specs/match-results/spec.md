# Match Results Specification

## Purpose

Define official match result entry and settlement triggering.

## Requirements

### Requirement: Official Match Result

The system MUST record one official outcome for a match: Team A, Draw, or Team B. A result MUST NOT be accepted before that match's betting window is closed. Once a settled match has a result, that result MUST NOT change balances again.

#### Scenario: Record result after betting closes

- GIVEN a match whose betting window is closed and has no official result
- WHEN an authorized user records Team A, Draw, or Team B as the result
- THEN the match stores that official result
- AND match settlement becomes eligible for that result

#### Scenario: Reject result before close

- GIVEN a match whose betting window is still open
- WHEN an official result is submitted
- THEN the result MUST NOT be recorded
- AND no settlement occurs

#### Scenario: Re-submit settled result

- GIVEN a match with a recorded result and completed settlement
- WHEN the same result is submitted again
- THEN balances and jackpot totals MUST remain unchanged
