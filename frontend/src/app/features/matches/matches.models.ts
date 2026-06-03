export interface MatchListItem {
	id: number;
	stage: string;
	homeTeamName: string;
	awayTeamName: string;
	groupName: string | null;
	startsAtUtc: string;
	bettingClosesAtUtc: string;
	isBettingOpen: boolean;
	stakeAmountCc: number;
	venue: string;
	currentUserBetSelection: MatchBetSelection | null;
	officialResult: MatchBetSelection | null;
	isSettled: boolean;
	settledAtUtc: string | null;
}

export type MatchBetSelection = "Home" | "Draw" | "Away";

export interface PlaceMatchBetRequest {
	matchId: number;
	selection: MatchBetSelection;
}

export interface PlaceMatchBetResult {
	matchId: number;
	selection: MatchBetSelection;
	stakeAmountCc: number;
	remainingBalanceCc: number;
	placedAtUtc: string;
}

export interface RecordMatchResultRequest {
	officialResult: MatchBetSelection;
}

export interface RecordMatchResult {
	matchId: number;
	officialResult: MatchBetSelection;
	wasAlreadySettled: boolean;
	winnersCount: number;
	losersCount: number;
	championJackpotContributionCc: number;
	championJackpotCc: number;
	settledAtUtc: string;
}

export interface ChampionBetMarket {
	teamOptions: ReadonlyArray<string>;
	stakeAmountCc: number;
	bettingClosesAtUtc: string | null;
	isBettingOpen: boolean;
	currentUserChampionTeamName: string | null;
}

export interface PlaceChampionBetRequest {
	teamName: string;
}

export interface PlaceChampionBetResult {
	teamName: string;
	stakeAmountCc: number;
	remainingBalanceCc: number;
	placedAtUtc: string;
}

export interface SettleChampionRequest {
	championTeamName: string;
}

export interface SettleChampionResult {
	championTeamName: string;
	wasAlreadySettled: boolean;
	winnersCount: number;
	losersCount: number;
	championJackpotCc: number;
	losingStakePoolCc: number;
	profitSharePerWinnerCc: number;
	totalPayoutPerWinnerCc: number;
	undistributedJackpotCc: number;
	settledAtUtc: string;
}

export interface CurrentUserSummary {
	id: number;
	displayName: string;
	email: string;
	currentBalanceCc: number;
	rescueCount: number;
	rescueDebtCc: number;
}

export interface FootballDataSnapshot {
	teams: ReadonlyArray<FootballTeam>;
	stadiums: ReadonlyArray<FootballStadium>;
	groupStandings: ReadonlyArray<FootballGroupStanding>;
	matches: ReadonlyArray<FootballMatch>;
	syncedAtUtc: string | null;
}

export interface FootballTeam {
	externalId: string;
	nameEn: string;
	fifaCode: string;
	iso2: string | null;
	groupName: string | null;
	flagUrl: string | null;
}

export interface FootballStadium {
	externalId: string;
	nameEn: string;
	fifaName: string | null;
	cityEn: string | null;
	countryEn: string | null;
	capacity: number | null;
	region: string | null;
}

export interface FootballGroupStanding {
	groupName: string;
	teamExternalId: string;
	played: number;
	won: number;
	drawn: number;
	lost: number;
	goalsFor: number;
	goalsAgainst: number;
	goalDifference: number;
	points: number;
}

export interface FootballMatch {
	externalId: string;
	homeTeamExternalId: string | null;
	awayTeamExternalId: string | null;
	homeTeamNameEn: string | null;
	awayTeamNameEn: string | null;
	homeTeamLabel: string | null;
	awayTeamLabel: string | null;
	groupName: string;
	matchday: string;
	localDateText: string;
	stadiumExternalId: string;
	isFinished: boolean;
	timeElapsed: string;
	stageType: string;
	homeScore: number | null;
	awayScore: number | null;
}

export interface SyncFootballDataResult {
	providerName: string;
	teamsCount: number;
	stadiumsCount: number;
	groupsCount: number;
	matchesCount: number;
	syncedAtUtc: string;
}

export interface ImportGroupStageFixturesResult {
	providerName: string;
	importedCount: number;
	updatedCount: number;
	skippedCount: number;
	unsafeUpdateSkippedCount: number;
	sourceSyncedAtUtc: string;
}
