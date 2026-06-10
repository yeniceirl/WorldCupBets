export type ChallengeStatus = "Open" | "Matched" | "Settled" | "Voided" | "Expired";
export type ChallengeSide = "Creator" | "Taker";
export interface ChallengePosition {
	userId: number;
	displayName: string;
	side: ChallengeSide;
	stakeAmountCc: number;
	escrowedAtUtc: string;
}
export interface MatchChallenge {
	id: number;
	matchId: number;
	claimText: string;
	creatorSideText: string;
	takerSideText: string;
	stakeAmountCc: number;
	status: ChallengeStatus;
	winnerSide: ChallengeSide | null;
	createdAtUtc: string;
	matchedAtUtc: string | null;
	settledAtUtc: string | null;
	voidedAtUtc: string | null;
	expiredAtUtc: string | null;
	positions: ReadonlyArray<ChallengePosition>;
}
export interface CreateChallengeRequest {
	matchId: number;
	claimText: string;
	creatorSideText: string;
	takerSideText: string;
	stakeAmountCc: number;
}
export interface ChallengeMutationResult {
	challenge: MatchChallenge;
	currentBalanceCc: number;
}
export interface SettleChallengeRequest {
	winnerSide: ChallengeSide;
}
