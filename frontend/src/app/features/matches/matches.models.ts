export interface MatchListItem {
	id: number;
	stage: string;
	homeTeamName: string;
	awayTeamName: string;
	startsAtUtc: string;
	bettingClosesAtUtc: string;
	isBettingOpen: boolean;
	stakeAmountCc: number;
	venue: string;
	currentUserBetSelection: MatchBetSelection | null;
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

export interface CurrentUserSummary {
	id: number;
	displayName: string;
	email: string;
	currentBalanceCc: number;
	rescueCount: number;
	rescueDebtCc: number;
}
