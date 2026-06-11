# Tournament Picks Specification

## Purpose

Unify tournament picks.

## Requirements

### Requirement: TournamentPick domain semantics

The system MUST use `TournamentPick` rows in `tournament_picks` with category, selected text, optional external id, stake, user, and placed timestamp. Category rules MUST remain explicit.

#### Scenario: Champion stores team
- GIVEN a user selects a champion team
- WHEN the pick is stored
- THEN category is `Champion`
- AND selected text is the team

#### Scenario: Player stores player
- GIVEN a user selects best player or top scorer
- WHEN the pick is stored
- THEN category is `BestPlayer` or `TopScorer`
- AND selected text is the player and external id MAY be set

### Requirement: One pick per user per category

The system MUST allow at most one pick per user/category in application/domain behavior and persistence.

#### Scenario: First pick is accepted
- GIVEN a user has no pick for a category
- WHEN the user places that category pick
- THEN the pick is accepted and persisted

#### Scenario: Duplicate is rejected
- GIVEN a user already has a pick for `Champion`
- WHEN the user places another champion pick
- THEN the request is rejected
- AND the existing pick is not changed

### Requirement: Migration preserves existing picks

The system SHALL migrate `champion_bets` and `special_player_bets` into `tournament_picks` without losing user, selection, category, stake, or timestamp, and SHALL support rollback.

#### Scenario: Champion rows migrate
- GIVEN champion bet rows
- WHEN migration runs forward
- THEN each becomes a `Champion` tournament pick
- AND team, stake, user, and timestamp are preserved

#### Scenario: Special player rows migrate
- GIVEN special player bet rows
- WHEN migration runs forward
- THEN each becomes `BestPlayer` or `TopScorer`
- AND player, external id, stake, user, and timestamp are preserved

### Requirement: Place flows remain compatible

The system MUST keep existing place-bet contracts while using tournament picks internally.

#### Scenario: Champion bet
- GIVEN champion betting is open and the user has no champion pick
- WHEN the existing champion bet endpoint is used
- THEN a `Champion` pick is created
- AND the response remains compatible

#### Scenario: Player bet
- GIVEN special player betting is open and the user has no category pick
- WHEN the existing player bet endpoint is used
- THEN a pick is created for the requested category
- AND category validation is applied

### Requirement: Markets expose pick state

The system MUST return compatible markets while deriving placed/pending state by category.

#### Scenario: Category state
- GIVEN a user has Champion and BestPlayer picks
- WHEN markets are requested
- THEN those categories are marked as already picked
- AND TopScorer remains available when absent

### Requirement: Pending stakes remain compatible

The system MUST include unsettled tournament-pick stakes in leaderboard and My Bets pending totals without double-counting settled champions.

#### Scenario: Pending stakes aggregate
- GIVEN a user has unsettled picks in multiple categories
- WHEN leaderboard or My Bets pending stakes are requested
- THEN each stake contributes once

#### Scenario: Settled champion not pending
- GIVEN champion settlement processed a champion pick
- WHEN pending stakes are requested
- THEN that champion pick no longer contributes as pending
- AND player-category picks remain pending

### Requirement: Champion settlement filters by category

The system MUST settle only `Champion` picks during champion settlement and MUST NOT settle player-category picks.

#### Scenario: Player categories excluded
- GIVEN picks exist for all categories
- WHEN champion settlement runs
- THEN only `Champion` picks are evaluated
- AND `BestPlayer` and `TopScorer` picks remain unsettled

### Requirement: Verification coverage

The system SHOULD test category semantics, uniqueness, migration, place flows, markets, pending stakes, and champion-only settlement.

#### Scenario: Backend verification command
- GIVEN implementation is complete
- WHEN verification runs
- THEN `dotnet test WorldCupBets.sln` SHOULD pass
- AND frontend build is only required if frontend files changed
